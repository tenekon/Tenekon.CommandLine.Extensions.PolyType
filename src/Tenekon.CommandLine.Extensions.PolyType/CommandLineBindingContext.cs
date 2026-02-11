using System.CommandLine;

namespace Tenekon.CommandLine.Extensions.PolyType;

internal sealed class CommandLineBindingContext(Dictionary<Type, CommandDescriptor> descriptors)
{
    private readonly Dictionary<Tuple<ParseResult, Type>, object> _bindCache = new();
    private readonly AsyncLocal<IServiceProvider?> _currentServiceProvider = new();

    public Dictionary<Type, Func<IServiceProvider?, object>> CreatorMap { get; } = new();
    public Dictionary<BinderKey, Action<object, ParseResult>> BinderMap { get; } = new();
    public Dictionary<Command, Type> CommandMap { get; } = new();
    public IServiceProvider? DefaultServiceProvider { get; set; }
    public IServiceProvider? CurrentServiceProvider
    {
        get => _currentServiceProvider.Value;
        set => _currentServiceProvider.Value = value;
    }

    public TDefinition Bind<TDefinition>(ParseResult parseResult, bool returnEmpty = false)
    {
        return (TDefinition)Bind(parseResult, typeof(TDefinition), serviceProvider: null, returnEmpty);
    }

    public object Bind(ParseResult parseResult, Type definitionType, bool returnEmpty = false)
    {
        return Bind(parseResult, definitionType, serviceProvider: null, returnEmpty);
    }

    public TDefinition Bind<TDefinition>(
        ParseResult parseResult,
        IServiceProvider? serviceProvider,
        bool returnEmpty = false)
    {
        return (TDefinition)Bind(parseResult, typeof(TDefinition), serviceProvider, returnEmpty);
    }

    public object Bind(
        ParseResult parseResult,
        Type definitionType,
        IServiceProvider? serviceProvider,
        bool returnEmpty = false)
    {
        serviceProvider ??= CurrentServiceProvider ?? DefaultServiceProvider;
        if (returnEmpty)
            return Create(definitionType, serviceProvider);

        var key = Tuple.Create(parseResult, definitionType);
        if (_bindCache.TryGetValue(key, out var existing))
            return existing;

        if (!descriptors.TryGetValue(definitionType, out var descriptor))
            throw new InvalidOperationException($"Command type '{definitionType.FullName}' is not registered.");

        if (descriptor.Parent is not null)
            Bind(parseResult, descriptor.Parent.DefinitionType, serviceProvider);

        var instance = Create(definitionType, serviceProvider);
        if (BinderMap.TryGetValue(new BinderKey(definitionType, definitionType), out var binder))
            binder(instance, parseResult);

        SetParentAccessors(descriptor, instance, parseResult);
        _bindCache[key] = instance;
        return instance;
    }

    public object BindCalled(ParseResult parseResult)
    {
        var type = GetCalledType(parseResult);
        return Bind(parseResult, type, serviceProvider: null);
    }

    public bool TryGetCalledType(ParseResult parseResult, out Type? value)
    {
        value = null;
        var command = parseResult.CommandResult?.Command;
        if (command is null) return false;

        return CommandMap.TryGetValue(command, out value);
    }

    public bool IsCalled<TDefinition>(ParseResult parseResult)
    {
        return IsCalled(parseResult, typeof(TDefinition));
    }

    public bool IsCalled(ParseResult parseResult, Type definitionType)
    {
        return TryGetCalledType(parseResult, out var calledType) && calledType == definitionType;
    }

    public bool Contains<TDefinition>(ParseResult parseResult)
    {
        return Contains(parseResult, typeof(TDefinition));
    }

    public bool Contains(ParseResult parseResult, Type definitionType)
    {
        return _bindCache.ContainsKey(Tuple.Create(parseResult, definitionType));
    }

    public object[] BindAll(ParseResult parseResult)
    {
        var list = new List<object>();
        foreach (var descriptor in descriptors.Values)
        {
            if (!IsInCalledHierarchy(parseResult, descriptor))
                continue;

            list.Add(Bind(parseResult, descriptor.DefinitionType, serviceProvider: null));
        }

        return list.ToArray();
    }

    private Type GetCalledType(ParseResult parseResult)
    {
        if (!TryGetCalledType(parseResult, out var type) || type is null)
            throw new InvalidOperationException("No called command was found for the current parse result.");

        return type;
    }

    private object Create(Type definitionType)
    {
        if (!CreatorMap.TryGetValue(definitionType, out var creator))
            throw new InvalidOperationException($"Creator is not found for command type '{definitionType.FullName}'.");

        return creator(CurrentServiceProvider ?? DefaultServiceProvider);
    }

    private object Create(Type definitionType, IServiceProvider? serviceProvider)
    {
        if (!CreatorMap.TryGetValue(definitionType, out var creator))
            throw new InvalidOperationException($"Creator is not found for command type '{definitionType.FullName}'.");

        return creator(serviceProvider);
    }

    private void SetParentAccessors(CommandDescriptor descriptor, object instance, ParseResult parseResult)
    {
        if (descriptor.Parent is null) return;

        foreach (var accessor in descriptor.ParentAccessors)
        {
            var parentType = accessor.ParentType;
            var parentInstance = Bind(parseResult, parentType, serviceProvider: null);
            accessor.Setter(instance, parentInstance);
        }
    }

    private bool IsInCalledHierarchy(ParseResult parseResult, CommandDescriptor descriptor)
    {
        if (!TryGetCalledType(parseResult, out var calledType) || calledType is null)
            return false;

        var current = descriptors[calledType];
        while (current is not null)
        {
            if (current.DefinitionType == descriptor.DefinitionType) return true;
            current = current.Parent;
        }

        return false;
    }
}

internal readonly record struct BinderKey(Type CommandType, Type TargetType);
