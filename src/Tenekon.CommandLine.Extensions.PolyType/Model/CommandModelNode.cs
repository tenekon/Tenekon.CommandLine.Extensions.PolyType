using PolyType;
using PolyType.Abstractions;
using Tenekon.CommandLine.Extensions.PolyType.Spec;

namespace Tenekon.CommandLine.Extensions.PolyType.Model;

internal sealed class CommandModelNode(Type definitionType, IObjectTypeShape shape, CommandSpecAttribute spec)
    : ICommandGraphNode
{
    private readonly List<SpecMemberAccessor> _specMembers = [];
    private readonly List<ParentAccessor> _parentAccessors = [];
    private readonly List<SpecEntry> _specEntries = [];
    private readonly List<Type> _interfaceTargets = [];
    private bool _initialized;

    public Type DefinitionType { get; } = definitionType;
    public IObjectTypeShape Shape { get; } = shape;
    public CommandSpecAttribute Spec { get; } = spec;
    public ICommandGraphNode? Parent { get; set; }
    public List<ICommandGraphNode> Children { get; } = [];
    public List<CommandMethodNode> MethodChildren { get; } = [];

    public IReadOnlyList<SpecMemberAccessor> SpecMembers => _specMembers;
    public IReadOnlyList<ParentAccessor> ParentAccessors => _parentAccessors;
    public IReadOnlyList<SpecEntry> SpecEntries => _specEntries;
    public IReadOnlyList<Type> InterfaceTargets => _interfaceTargets;

    public string DisplayName => DefinitionType.Name;
    public Type? CommandType => DefinitionType;

    public ICommandGraphNode GetRoot()
    {
        ICommandGraphNode current = this;
        while (current.Parent is not null)
            current = current.Parent;
        return current;
    }

    public CommandModelNode? Find(Type type)
    {
        if (DefinitionType == type) return this;
        foreach (var child in Children.OfType<CommandModelNode>())
        {
            var found = child.Find(type);
            if (found is not null) return found;
        }

        foreach (var method in MethodChildren)
        foreach (var child in method.Children.OfType<CommandModelNode>())
        {
            var found = child.Find(type);
            if (found is not null) return found;
        }

        return null;
    }

    public void InitializeModel()
    {
        if (_initialized) return;
        _initialized = true;

        var members = CollectSpecMembers(Shape, out var interfaceTargets)
            .OrderBy(entry => entry.Option?.Order ?? entry.Argument?.Order ?? entry.Directive?.Order ?? 0)
            .ThenBy(entry => entry.Property.Position)
            .ToList();

        _specEntries.AddRange(members);
        _interfaceTargets.AddRange(interfaceTargets);

        foreach (var entry in members)
        {
            if (entry.Option is null && entry.Argument is null) continue;
            var getter = PropertyAccessorFactory.CreateGetter(entry.Property);
            _specMembers.Add(new SpecMemberAccessor(entry.Property.Name, getter ?? (_ => null)));
        }

        BuildParentAccessors(interfaceTargets);
    }

    private void BuildParentAccessors(HashSet<Type> interfaceTargets)
    {
        var ancestor = Parent;
        if (ancestor is null) return;

        foreach (var property in Shape.Properties)
        {
            if (property.AttributeProvider.IsDefined<OptionSpecAttribute>()) continue;
            if (property.AttributeProvider.IsDefined<ArgumentSpecAttribute>()) continue;
            if (property.AttributeProvider.IsDefined<DirectiveSpecAttribute>()) continue;

            var propertyType = property.PropertyType.Type;
            if (interfaceTargets.Contains(propertyType)) continue;
            while (ancestor is not null)
            {
                if (ancestor is CommandModelNode ancestorNode && propertyType == ancestorNode.DefinitionType)
                {
                    var setter = PropertyAccessorFactory.CreateSetter(property);
                    if (setter is not null) _parentAccessors.Add(new ParentAccessor(propertyType, setter));

                    break;
                }

                ancestor = ancestor.Parent;
            }
        }
    }

    private static IReadOnlyList<SpecEntry> CollectSpecMembers(
        IObjectTypeShape shape,
        out HashSet<Type> interfaceTargets)
    {
        var interfaceMap = new Dictionary<string, SpecEntry>(StringComparer.Ordinal);
        var classPropertiesByName = shape.Properties.GroupBy(property => property.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var interfaceTypes = new HashSet<Type>();
        foreach (var iface in shape.Type.GetInterfaces())
        {
            if (shape.Provider.GetTypeShape(iface) is not IObjectTypeShape ifaceShape)
            {
                if (InterfaceDefinesSpecs(iface))
                    throw new InvalidOperationException(
                        $"Interface '{iface.FullName}' is not shapeable. Add a PolyType shape for it.");

                continue;
            }

            var hasInterfaceSpecs = false;
            foreach (var property in ifaceShape.Properties)
            {
                classPropertiesByName.TryGetValue(property.Name, out var valueProperty);
                var entry = CreateSpecEntry(property, iface, valueProperty);
                if (entry is null) continue;
                hasInterfaceSpecs = true;

                if (interfaceMap.TryGetValue(property.Name, out var existing))
                    throw new InvalidOperationException(
                        $"Multiple interfaces provide specs for '{property.Name}' on '{shape.Type.FullName}'.");

                interfaceMap[property.Name] = entry;
            }

            if (hasInterfaceSpecs) interfaceTypes.Add(iface);
        }

        var classEntries = new List<SpecEntry>();
        foreach (var property in shape.Properties)
        {
            var entry = CreateSpecEntry(property, shape.Type, property);
            if (entry is null) continue;

            if (interfaceMap.ContainsKey(property.Name))
                throw new InvalidOperationException(
                    $"Property '{property.Name}' on '{shape.Type.FullName}' conflicts with interface spec.");

            classEntries.Add(entry);
        }

        interfaceTargets = interfaceTypes;
        return interfaceMap.Values.Concat(classEntries).ToList();
    }

    private static bool InterfaceDefinesSpecs(Type interfaceType)
    {
        foreach (var property in interfaceType.GetProperties())
        {
            if (property.IsDefined(typeof(OptionSpecAttribute), inherit: true)) return true;
            if (property.IsDefined(typeof(ArgumentSpecAttribute), inherit: true)) return true;
            if (property.IsDefined(typeof(DirectiveSpecAttribute), inherit: true)) return true;
        }

        return false;
    }

    private static SpecEntry? CreateSpecEntry(IPropertyShape property, Type ownerType, IPropertyShape? valueProperty)
    {
        var option = property.AttributeProvider.GetCustomAttribute<OptionSpecAttribute>();
        var argument = property.AttributeProvider.GetCustomAttribute<ArgumentSpecAttribute>();
        var directive = property.AttributeProvider.GetCustomAttribute<DirectiveSpecAttribute>();
        if (option is null && argument is null && directive is null) return null;

        return new SpecEntry(ownerType, property, valueProperty ?? property, option, argument, directive);
    }

    internal static CommandModelNode GetDescriptor(
        ITypeShapeProvider provider,
        Dictionary<Type, CommandModelNode> descriptors,
        Type type)
    {
        if (descriptors.TryGetValue(type, out var existing)) return existing;

        var shape = provider.GetTypeShape(type) as IObjectTypeShape
            ?? throw new InvalidOperationException($"Type '{type.FullName}' is not shapeable.");

        var spec = shape.AttributeProvider.GetCustomAttribute<CommandSpecAttribute>();
        if (spec is null) throw new InvalidOperationException($"Type '{type.FullName}' is missing [CommandSpec].");

        var descriptor = new CommandModelNode(type, shape, spec);
        descriptors[type] = descriptor;
        return descriptor;
    }

    internal sealed record SpecEntry(
        Type OwnerType,
        IPropertyShape Property,
        IPropertyShape ValueProperty,
        OptionSpecAttribute? Option,
        ArgumentSpecAttribute? Argument,
        DirectiveSpecAttribute? Directive);
}