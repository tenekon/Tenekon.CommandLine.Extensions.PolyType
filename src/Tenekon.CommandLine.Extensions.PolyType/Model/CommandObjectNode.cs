using PolyType;
using PolyType.Abstractions;
using Tenekon.CommandLine.Extensions.PolyType.Spec;

namespace Tenekon.CommandLine.Extensions.PolyType.Model;

internal sealed class CommandObjectNode(
    Type definitionType,
    IObjectTypeShape shape,
    CommandSpecModel spec,
    CommandHandlerConventionSpecModel? handlerConvention = null) : ICommandGraphNode
{
    private readonly List<SpecMemberEntry> _specMembers = [];
    private readonly List<ParentAccessorEntry> _parentAccessors = [];
    private readonly List<SpecEntry> _specEntries = [];
    private readonly List<Type> _interfaceTargets = [];
    private bool _initialized;

    public Type DefinitionType { get; } = definitionType;
    public IObjectTypeShape Shape { get; } = shape;
    public CommandSpecModel Spec { get; } = spec;

    public CommandHandlerConventionSpecModel HandlerConvention { get; } = handlerConvention
        ?? CommandHandlerConventionSpecModel.CreateDefault();

    public ICommandGraphNode? Parent { get; internal set; }
    public List<ICommandGraphNode> Children { get; } = [];
    public List<CommandMethodNode> MethodChildren { get; } = [];

    public IReadOnlyList<SpecMemberEntry> SpecMembers => _specMembers;
    public IReadOnlyList<ParentAccessorEntry> ParentAccessors => _parentAccessors;
    public IReadOnlyList<SpecEntry> SpecEntries => _specEntries;
    public IReadOnlyList<Type> InterfaceTargets => _interfaceTargets;

    public string DisplayName => DefinitionType.Name;
    public Type? CommandType => DefinitionType;

    IReadOnlyList<ICommandGraphNode> ICommandGraphNode.Children => Children;

    public ICommandGraphNode GetRoot()
    {
        ICommandGraphNode current = this;
        while (current.Parent is not null)
            current = current.Parent;
        return current;
    }

    public CommandObjectNode? Find(Type type)
    {
        if (DefinitionType == type) return this;
        foreach (var child in Children.OfType<CommandObjectNode>())
        {
            var found = child.Find(type);
            if (found is not null) return found;
        }

        foreach (var method in MethodChildren)
        foreach (var child in method.Children.OfType<CommandObjectNode>())
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

        var specPropertyNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in _specEntries)
        {
            if (entry.OwnerType.IsInterface)
                if (!_interfaceTargets.Contains(entry.OwnerType))
                    _interfaceTargets.Add(entry.OwnerType);

            if (entry.Option is not null || entry.Argument is not null || entry.Directive is not null)
                specPropertyNames.Add(entry.SpecProperty.Name);
        }

        var orderedEntries = _specEntries
            .OrderBy(entry => entry.Option?.Order ?? entry.Argument?.Order ?? entry.Directive?.Order ?? 0)
            .ThenBy(entry => entry.SpecProperty.Position)
            .ToList();

        foreach (var entry in orderedEntries)
        {
            if (entry.Option is null && entry.Argument is null) continue;
            _specMembers.Add(new SpecMemberEntry(entry.SpecProperty.Name, entry.SpecProperty));
        }

        BuildParentAccessors([.._interfaceTargets], specPropertyNames);
    }

    internal void SetSpecEntries(IEnumerable<SpecEntry> entries)
    {
        if (_initialized)
            throw new InvalidOperationException("Spec entries cannot be modified after initialization.");

        _specEntries.Clear();
        _specEntries.AddRange(entries);
    }

    private void BuildParentAccessors(HashSet<Type> interfaceTargets, HashSet<string> specPropertyNames)
    {
        var ancestor = Parent;
        if (ancestor is null) return;

        foreach (var property in Shape.Properties)
        {
            if (specPropertyNames.Contains(property.Name)) continue;

            var propertyType = property.PropertyType.Type;
            if (interfaceTargets.Contains(propertyType)) continue;
            while (ancestor is not null)
            {
                if (ancestor is CommandObjectNode ancestorNode && propertyType == ancestorNode.DefinitionType)
                {
                    _parentAccessors.Add(new ParentAccessorEntry(propertyType, property));

                    break;
                }

                ancestor = ancestor.Parent;
            }
        }
    }

    internal static IReadOnlyList<SpecEntry> CollectSpecMembers(IObjectTypeShape shape)
    {
        var interfaceMap = new Dictionary<string, SpecEntry>(StringComparer.Ordinal);
        var classPropertiesByName = shape.Properties.GroupBy(property => property.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        foreach (var iface in shape.Type.GetInterfaces())
        {
            if (shape.Provider.GetTypeShape(iface) is not IObjectTypeShape ifaceShape)
            {
                if (InterfaceDefinesSpecs(iface))
                    throw new InvalidOperationException(
                        $"Interface '{iface.FullName}' is not shapeable. Add a PolyType shape for it.");

                continue;
            }

            foreach (var property in ifaceShape.Properties)
            {
                classPropertiesByName.TryGetValue(property.Name, out var targetProperty);
                var entry = CreateSpecEntry(property, iface, targetProperty);
                if (entry is null) continue;

                if (interfaceMap.TryGetValue(property.Name, out var existing))
                {
                    // Allow duplicates from interface inheritance; prefer the most derived interface.
                    if (existing.OwnerType.IsAssignableFrom(iface) || iface.IsAssignableFrom(existing.OwnerType))
                    {
                        if (existing.OwnerType.IsAssignableFrom(iface))
                            interfaceMap[property.Name] = entry;

                        continue;
                    }

                    throw new InvalidOperationException(
                        $"Multiple interfaces provide specs for '{property.Name}' on '{shape.Type.FullName}'.");
                }

                interfaceMap[property.Name] = entry;
            }
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

    private static SpecEntry? CreateSpecEntry(IPropertyShape specProperty, Type ownerType, IPropertyShape? targetProperty)
    {
        var option = specProperty.AttributeProvider.GetCustomAttribute<OptionSpecAttribute>();
        var argument = specProperty.AttributeProvider.GetCustomAttribute<ArgumentSpecAttribute>();
        var directive = specProperty.AttributeProvider.GetCustomAttribute<DirectiveSpecAttribute>();
        if (option is null && argument is null && directive is null) return null;

        return new SpecEntry(
            ownerType,
            specProperty,
            targetProperty ?? specProperty,
            option is null ? null : OptionSpecModel.FromAttribute(option),
            argument is null ? null : ArgumentSpecModel.FromAttribute(argument),
            directive is null ? null : DirectiveSpecModel.FromAttribute(directive));
    }

    internal static CommandObjectNode GetDescriptor(
        ITypeShapeProvider provider,
        Dictionary<Type, CommandObjectNode> descriptors,
        Type type)
    {
        if (descriptors.TryGetValue(type, out var existing)) return existing;

        var shape = provider.GetTypeShape(type) as IObjectTypeShape
            ?? throw new InvalidOperationException($"Type '{type.FullName}' is not shapeable.");

        var specAttribute = shape.AttributeProvider.GetCustomAttribute<CommandSpecAttribute>();
        if (specAttribute is null)
            throw new InvalidOperationException($"Type '{type.FullName}' is missing [CommandSpec].");
        var spec = CommandSpecModel.FromAttribute(specAttribute);

        var descriptor = new CommandObjectNode(type, shape, spec, CommandHandlerConventionSpecModel.CreateDefault());
        descriptors[type] = descriptor;
        return descriptor;
    }

    internal sealed record SpecEntry(
        Type OwnerType,
        IPropertyShape SpecProperty,
        IPropertyShape TargetProperty,
        OptionSpecModel? Option,
        ArgumentSpecModel? Argument,
        DirectiveSpecModel? Directive);

    internal sealed record SpecMemberEntry(string DisplayName, IPropertyShape SpecProperty);

    internal sealed record ParentAccessorEntry(Type ParentType, IPropertyShape Property);
}
