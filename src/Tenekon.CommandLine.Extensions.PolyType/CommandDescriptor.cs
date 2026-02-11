using System.CommandLine;
using System.CommandLine.Help;
using PolyType;
using PolyType.Abstractions;

namespace Tenekon.CommandLine.Extensions.PolyType;

internal sealed class CommandDescriptor(Type definitionType, IObjectTypeShape shape, CommandSpecAttribute spec)
{
    private readonly List<SpecMember> _specMembers = [];
    private readonly List<ParentAccessor> _parentAccessors = [];

    public Type DefinitionType { get; } = definitionType;
    public IObjectTypeShape Shape { get; } = shape;
    public CommandSpecAttribute Spec { get; } = spec;
    public CommandDescriptor? Parent { get; set; }
    public List<CommandDescriptor> Children { get; } = [];
    public Command? Command { get; private set; }
    public IReadOnlyList<SpecMember> SpecMembers => _specMembers;
    public IReadOnlyList<ParentAccessor> ParentAccessors => _parentAccessors;

    public string DisplayName => Command?.Name ?? DefinitionType.Name;

    public CommandDescriptor GetRoot()
    {
        var current = this;
        while (current.Parent is not null)
            current = current.Parent;
        return current;
    }

    public CommandDescriptor? Find(Type type)
    {
        if (DefinitionType == type) return this;
        foreach (var child in Children)
        {
            var found = child.Find(type);
            if (found is not null) return found;
        }

        return null;
    }

    public void LinkRelationships(ITypeShapeProvider provider, Dictionary<Type, CommandDescriptor> descriptors)
    {
        foreach (var descriptor in descriptors.Values)
        {
            var declaringType = descriptor.DefinitionType.DeclaringType;
            if (descriptor.Parent is null && declaringType is not null
                && descriptors.TryGetValue(declaringType, out var nestedParent))
            {
                descriptor.Parent = nestedParent;
                if (!nestedParent.Children.Contains(descriptor))
                    nestedParent.Children.Add(descriptor);
            }

            if (descriptor.Spec.Parent is not null)
            {
                var parentDescriptor = GetDescriptor(provider, descriptors, descriptor.Spec.Parent);
                if (descriptor.Parent is not null
                    && descriptor.Parent.DefinitionType != parentDescriptor.DefinitionType)
                    throw new InvalidOperationException(
                        $"Command '{descriptor.DefinitionType.FullName}' has conflicting parents.");

                descriptor.Parent = parentDescriptor;
                if (!parentDescriptor.Children.Contains(descriptor))
                    parentDescriptor.Children.Add(descriptor);
            }

            if (descriptor.Spec.Children is not null)
                foreach (var childType in descriptor.Spec.Children)
                {
                    if (childType is null) continue;
                    var childDescriptor = GetDescriptor(provider, descriptors, childType);
                    if (childDescriptor.Parent is not null
                        && childDescriptor.Parent.DefinitionType != descriptor.DefinitionType)
                        throw new InvalidOperationException(
                            $"Command '{childDescriptor.DefinitionType.FullName}' has conflicting parents.");

                    childDescriptor.Parent = descriptor;
                    if (!descriptor.Children.Contains(childDescriptor))
                        descriptor.Children.Add(childDescriptor);
                }
        }
    }

    public void ValidateNoCycles()
    {
        var visited = new HashSet<CommandDescriptor>();
        var stack = new HashSet<CommandDescriptor>();

        void Visit(CommandDescriptor node)
        {
            if (stack.Contains(node))
                throw new InvalidOperationException(
                    $"Command hierarchy has a cycle at '{node.DefinitionType.FullName}'.");

            if (visited.Contains(node)) return;
            visited.Add(node);
            stack.Add(node);

            foreach (var child in node.Children)
                Visit(child);

            stack.Remove(node);
        }

        Visit(GetRoot());
    }

    public void BuildCommands(
        CommandLineBindingContext bindingContext,
        CommandLineSettings settings,
        IServiceProvider? serviceProvider,
        CommandLineNamer? parentNamer,
        CommandDescriptor rootDescriptor)
    {
        var namer = new CommandLineNamer(
            Spec.IsNameAutoGenerateSpecified ? Spec.NameAutoGenerate : null,
            Spec.IsNameCasingConventionSpecified ? Spec.NameCasingConvention : null,
            Spec.IsNamePrefixConventionSpecified ? Spec.NamePrefixConvention : null,
            Spec.IsShortFormAutoGenerateSpecified ? Spec.ShortFormAutoGenerate : null,
            Spec.IsShortFormPrefixConventionSpecified ? Spec.ShortFormPrefixConvention : null,
            parentNamer);

        Command command;
        if (Parent is null)
        {
            command = new RootCommand();
            AddBuiltInSymbols((RootCommand)command, settings);
        }
        else
        {
            var commandName = namer.GetCommandName(DefinitionType.Name, Spec.Name);
            command = new Command(commandName);
            TryAddAliases(command, Spec.Alias, Spec.Aliases, namer, isOption: false);
            var shortForm = namer.CreateShortForm(commandName, forOption: false);
            if (!string.IsNullOrWhiteSpace(shortForm)) command.Aliases.Add(shortForm);
        }

        if (!string.IsNullOrWhiteSpace(Spec.Description))
            command.Description = Spec.Description;

        command.Hidden = Spec.Hidden;
        command.TreatUnmatchedTokensAsErrors = Spec.TreatUnmatchedTokensAsErrors;

        Command = command;
        bindingContext.CommandMap[command] = DefinitionType;

        var creator = CreateInstanceFactory();
        bindingContext.CreatorMap[DefinitionType] = creator;

        var defaultInstance = creator(serviceProvider);
        BuildMembers(command, bindingContext, namer, defaultInstance, rootDescriptor);

        AddHandler(command, bindingContext, settings, rootDescriptor, serviceProvider);

        foreach (var child in Children.OrderBy(static child => child.Spec.Order)
                     .ThenBy(static child => child.DefinitionType.Name, StringComparer.Ordinal))
        {
            child.BuildCommands(bindingContext, settings, serviceProvider, namer, rootDescriptor);
            if (child.Command is not null)
                command.Add(child.Command);
        }
    }

    private void BuildMembers(
        Command command,
        CommandLineBindingContext bindingContext,
        CommandLineNamer namer,
        object defaultInstance,
        CommandDescriptor rootDescriptor)
    {
        var members = CollectSpecMembers(Shape, out var interfaceTargets)
            .OrderBy(entry => entry.Option?.Order ?? entry.Argument?.Order ?? entry.Directive?.Order ?? 0)
            .ThenBy(entry => entry.Property.Position)
            .ToList();
        var targets = new List<Type> { DefinitionType };
        targets.AddRange(interfaceTargets);

        foreach (var entry in members)
        {
            if (entry.Option is not null)
            {
                BuildOption(
                    entry.Property,
                    entry.ValueProperty,
                    entry.Option,
                    command,
                    bindingContext,
                    namer,
                    defaultInstance,
                    targets,
                    entry.OwnerType);
                continue;
            }

            if (entry.Argument is not null)
            {
                BuildArgument(
                    entry.Property,
                    entry.ValueProperty,
                    entry.Argument,
                    command,
                    bindingContext,
                    namer,
                    defaultInstance,
                    targets,
                    entry.OwnerType);
                continue;
            }

            if (entry.Directive is not null)
                BuildDirective(
                    entry.Property,
                    entry.Directive,
                    rootDescriptor,
                    bindingContext,
                    namer,
                    targets,
                    entry.OwnerType);
        }

        BuildParentAccessors(interfaceTargets);
    }

    private void BuildOption(
        IPropertyShape propertyShape,
        IPropertyShape valueProperty,
        OptionSpecAttribute spec,
        Command command,
        CommandLineBindingContext bindingContext,
        CommandLineNamer namer,
        object defaultInstance,
        IReadOnlyList<Type> targets,
        Type ownerType)
    {
        var builder = new OptionMemberBuilder(propertyShape, valueProperty, spec, namer, defaultInstance);
        var result = builder.Build();
        if (result is null) return;

        command.Add((Option)result.Symbol);
        AddBinders(bindingContext, targets, result.Binder, ownerType);

        _specMembers.Add(result.Member);
    }

    private void BuildArgument(
        IPropertyShape propertyShape,
        IPropertyShape valueProperty,
        ArgumentSpecAttribute spec,
        Command command,
        CommandLineBindingContext bindingContext,
        CommandLineNamer namer,
        object defaultInstance,
        IReadOnlyList<Type> targets,
        Type ownerType)
    {
        var builder = new ArgumentMemberBuilder(propertyShape, valueProperty, spec, namer, defaultInstance);
        var result = builder.Build();
        if (result is null) return;

        command.Add((Argument)result.Symbol);
        AddBinders(bindingContext, targets, result.Binder, ownerType);

        _specMembers.Add(result.Member);
    }

    private void BuildDirective(
        IPropertyShape propertyShape,
        DirectiveSpecAttribute spec,
        CommandDescriptor rootDescriptor,
        CommandLineBindingContext bindingContext,
        CommandLineNamer namer,
        IReadOnlyList<Type> targets,
        Type ownerType)
    {
        if (rootDescriptor.Command is not RootCommand rootCommand)
            return;

        var builder = new DirectiveMemberBuilder(propertyShape, spec, namer);
        var result = builder.Build();
        if (result is null) return;

        rootCommand.Add(result.Directive);
        AddBinders(bindingContext, targets, result.Binder, ownerType);
    }

    private void AddHandler(
        Command command,
        CommandLineBindingContext bindingContext,
        CommandLineSettings settings,
        CommandDescriptor rootDescriptor,
        IServiceProvider? serviceProvider)
    {
        var handler = CommandHandlerFactory.TryCreateHandler(Shape, bindingContext, settings, rootDescriptor);
        if (handler is null) return;

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            return await handler.InvokeAsync(parseResult, bindingContext.CurrentServiceProvider, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
        });
    }

    private void BuildParentAccessors(HashSet<Type> interfaceTargets)
    {
        if (Parent is null) return;

        foreach (var property in Shape.Properties)
        {
            if (property.AttributeProvider.IsDefined<OptionSpecAttribute>()) continue;
            if (property.AttributeProvider.IsDefined<ArgumentSpecAttribute>()) continue;
            if (property.AttributeProvider.IsDefined<DirectiveSpecAttribute>()) continue;

            var propertyType = property.PropertyType.Type;
            if (interfaceTargets.Contains(propertyType)) continue;
            var ancestor = Parent;
            while (ancestor is not null)
            {
                if (propertyType == ancestor.DefinitionType)
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
                {
                    throw new InvalidOperationException(
                        $"Interface '{iface.FullName}' is not shapeable. Add a PolyType shape for it.");
                }

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
                {
                    throw new InvalidOperationException(
                        $"Multiple interfaces provide specs for '{property.Name}' on '{shape.Type.FullName}'.");
                }

                interfaceMap[property.Name] = entry;
            }

            if (hasInterfaceSpecs)
                interfaceTypes.Add(iface);
        }

        var classEntries = new List<SpecEntry>();
        foreach (var property in shape.Properties)
        {
            var entry = CreateSpecEntry(property, shape.Type, property);
            if (entry is null) continue;

            if (interfaceMap.ContainsKey(property.Name))
            {
                throw new InvalidOperationException(
                    $"Property '{property.Name}' on '{shape.Type.FullName}' conflicts with interface spec.");
            }

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

    private void AddBinders(
        CommandLineBindingContext bindingContext,
        IReadOnlyList<Type> targets,
        Action<object, ParseResult> binder,
        Type ownerType)
    {
        foreach (var target in targets)
        {
            if (!ownerType.IsAssignableFrom(target)) continue;
            var key = new BinderKey(DefinitionType, target);
            if (bindingContext.BinderMap.TryGetValue(key, out var existing))
            {
                bindingContext.BinderMap[key] = (instance, parseResult) =>
                {
                    existing(instance, parseResult);
                    binder(instance, parseResult);
                };
            }
            else
            {
                bindingContext.BinderMap[key] = binder;
            }
        }
    }

    private sealed record SpecEntry(
        Type OwnerType,
        IPropertyShape Property,
        IPropertyShape ValueProperty,
        OptionSpecAttribute? Option,
        ArgumentSpecAttribute? Argument,
        DirectiveSpecAttribute? Directive);

    private Func<IServiceProvider?, object> CreateInstanceFactory()
    {
        var constructor = Shape.Constructor;
        if (constructor is null)
            throw new InvalidOperationException(
                $"Type '{DefinitionType.FullName}' does not expose a constructor shape.");

        var factory = new ConstructorFactory();
        var creator = constructor.Accept(factory) as Func<IServiceProvider?, object>;
        if (creator is null)
            throw new InvalidOperationException($"Unable to create factory for '{DefinitionType.FullName}'.");

        return creator;
    }

    private static void AddBuiltInSymbols(RootCommand rootCommand, CommandLineSettings settings)
    {
        rootCommand.Add(new HelpOption());
        rootCommand.Add(new VersionOption());

        if (settings.EnableSuggestDirective)
            rootCommand.Add(new System.CommandLine.Completions.SuggestDirective());
        if (settings.EnableDiagramDirective)
            rootCommand.Add(new DiagramDirective());
        if (settings.EnableEnvironmentVariablesDirective)
            rootCommand.Add(new EnvironmentVariablesDirective());
    }

    private static CommandDescriptor GetDescriptor(
        ITypeShapeProvider provider,
        Dictionary<Type, CommandDescriptor> descriptors,
        Type type)
    {
        if (descriptors.TryGetValue(type, out var existing)) return existing;

        var shape = provider.GetTypeShape(type) as IObjectTypeShape
            ?? throw new InvalidOperationException($"Type '{type.FullName}' is not shapeable.");

        var spec = shape.AttributeProvider.GetCustomAttribute<CommandSpecAttribute>();
        if (spec is null)
            throw new InvalidOperationException($"Type '{type.FullName}' is missing [CommandSpec].");

        var descriptor = new CommandDescriptor(type, shape, spec);
        descriptors[type] = descriptor;
        return descriptor;
    }

    private static void TryAddAliases(
        Command command,
        string? alias,
        string[]? aliases,
        CommandLineNamer namer,
        bool isOption)
    {
        if (!string.IsNullOrWhiteSpace(alias))
        {
            namer.AddAlias(alias);
            command.Aliases.Add(alias);
        }

        if (aliases is null) return;
        foreach (var entry in aliases)
        {
            if (string.IsNullOrWhiteSpace(entry)) continue;
            namer.AddAlias(entry);
            command.Aliases.Add(entry);
        }
    }

    internal readonly record struct SpecMember(string DisplayName, Func<object, object?> Getter);

    internal readonly record struct ParentAccessor(Type ParentType, Action<object, object> Setter);
}