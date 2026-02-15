using System.CommandLine;
using PolyType.Abstractions;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.Graph;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.Binding;

internal sealed class BindingContext(BindingRegistry registry, CommandRuntimeSettings settings)
{
    private readonly Dictionary<Tuple<ParseResult, Type>, object> _bindCache = new();
    private readonly Dictionary<Command, RuntimeNode> _commandMap = new();
    private readonly AsyncLocal<ICommandServiceResolver?> _currentServiceResolver = new();
    private Dictionary<Type, RuntimeNode>? _descriptors;
    private RuntimeNode? _rootNode;

    public Dictionary<Type, Func<BindingContext, ParseResult, ICommandServiceResolver?, CancellationToken, object>>
        CreatorMap =>
        registry.CreatorMap;

    public Dictionary<BinderKey, Action<object, ParseResult>> BinderMap => registry.BinderMap;
    public BindingRegistry Registry => registry;
    public CommandRuntimeSettings Settings { get; } = settings;
    public ICommandServiceResolver? DefaultServiceResolver { get; set; }
    public ICommandFunctionResolver? DefaultFunctionResolver { get; set; }
    public CommandFunctionRegistry FunctionRegistry { get; } = new();
    public bool AllowFunctionResolutionFromServices { get; set; }

    public ICommandServiceResolver? CurrentServiceResolver
    {
        get => _currentServiceResolver.Value;
        set => _currentServiceResolver.Value = value;
    }

    public RuntimeNode RootNode =>
        _rootNode ?? throw new InvalidOperationException("Binding context is not initialized.");

    public void Initialize(RuntimeNode rootNode)
    {
        _rootNode = rootNode;
        _descriptors = BuildDescriptorMap(rootNode);
        _commandMap.Clear();
        BuildCommandMap(rootNode, _commandMap);
    }

    public TDefinition Bind<TDefinition>(
        ParseResult parseResult,
        bool returnEmpty = false,
        CancellationToken cancellationToken = default)
    {
        return (TDefinition)Bind(
            parseResult,
            typeof(TDefinition),
            serviceResolver: null,
            returnEmpty,
            cancellationToken);
    }

    public object Bind(
        ParseResult parseResult,
        Type definitionType,
        bool returnEmpty = false,
        CancellationToken cancellationToken = default)
    {
        return Bind(parseResult, definitionType, serviceResolver: null, returnEmpty, cancellationToken);
    }

    public TDefinition Bind<TDefinition>(
        ParseResult parseResult,
        ICommandServiceResolver? serviceResolver,
        bool returnEmpty = false,
        CancellationToken cancellationToken = default)
    {
        return (TDefinition)Bind(parseResult, typeof(TDefinition), serviceResolver, returnEmpty, cancellationToken);
    }

    public object Bind(
        ParseResult parseResult,
        Type definitionType,
        ICommandServiceResolver? serviceResolver,
        bool returnEmpty = false,
        CancellationToken cancellationToken = default)
    {
        serviceResolver ??= CurrentServiceResolver ?? DefaultServiceResolver;
        var key = Tuple.Create(parseResult, definitionType);
        if (_bindCache.TryGetValue(key, out var existing)) return existing;

        var descriptorMap = GetDescriptorMap();
        if (!descriptorMap.TryGetValue(definitionType, out var descriptor))
            throw new InvalidOperationException($"Command type '{definitionType.FullName}' is not registered.");

        if (returnEmpty && descriptor.Kind != RuntimeNodeKind.Function)
            return Create(parseResult, definitionType, serviceResolver, cancellationToken);

        if (descriptor.Parent is { DefinitionType: { } parentType })
            Bind(parseResult, parentType, serviceResolver, returnEmpty: false, cancellationToken);

        if (descriptor.Kind == RuntimeNodeKind.Function)
        {
            var functionResolver = GetInvocationFunctionResolver(serviceResolver)
                ?? CreateFunctionResolver(serviceResolver, overrideResolver: null);
            var functionShape = descriptor.FunctionShape
                ?? throw new InvalidOperationException("Command function shape is not available.");
            if (!TryResolveFunctionInstance(functionShape, functionResolver, out var functionInstance)
                || functionInstance is null)
                throw new InvalidOperationException(
                    $"Function instance is not registered for '{definitionType.FullName}'.");

            if (!returnEmpty) _bindCache[key] = functionInstance;
            return functionInstance;
        }

        var instance = Create(parseResult, definitionType, serviceResolver, cancellationToken);
        if (BinderMap.TryGetValue(new BinderKey(definitionType, definitionType), out var binder))
            binder(instance, parseResult);

        SetParentAccessors(descriptor, instance, parseResult, serviceResolver, cancellationToken);
        _bindCache[key] = instance;
        return instance;
    }

    public object BindCalled(ParseResult parseResult)
    {
        var type = GetCalledType(parseResult);
        return Bind(parseResult, type, serviceResolver: null, returnEmpty: false, cancellationToken: default);
    }

    public bool TryGetCalledType(ParseResult parseResult, out Type? value)
    {
        value = null;
        var command = parseResult.CommandResult?.Command;
        if (command is null) return false;

        if (!_commandMap.TryGetValue(command, out var node)) return false;
        if (node.DefinitionType is null) return false;

        value = node.DefinitionType;
        return true;
    }

    internal bool TryGetCalledNode(ParseResult parseResult, out RuntimeNode? node)
    {
        node = null;
        var command = parseResult.CommandResult?.Command;
        if (command is null) return false;

        return _commandMap.TryGetValue(command, out node);
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
        foreach (var descriptor in GetDescriptorMap().Values)
        {
            if (!IsInCalledHierarchy(parseResult, descriptor)) continue;

            list.Add(
                Bind(
                    parseResult,
                    descriptor.DefinitionType,
                    serviceResolver: null,
                    returnEmpty: false,
                    cancellationToken: default));
        }

        return list.ToArray();
    }

    private Type GetCalledType(ParseResult parseResult)
    {
        if (!TryGetCalledType(parseResult, out var type) || type is null)
            throw new InvalidOperationException("No called command was found for the current parse result.");

        return type;
    }

    private object Create(
        ParseResult parseResult,
        Type definitionType,
        ICommandServiceResolver? serviceResolver,
        CancellationToken cancellationToken)
    {
        if (!CreatorMap.TryGetValue(definitionType, out var creator))
            throw new InvalidOperationException($"Creator is not found for command type '{definitionType.FullName}'.");

        return creator(
            this,
            parseResult,
            serviceResolver ?? CurrentServiceResolver ?? DefaultServiceResolver,
            cancellationToken);
    }

    private void SetParentAccessors(
        RuntimeNode descriptor,
        object instance,
        ParseResult parseResult,
        ICommandServiceResolver? serviceResolver,
        CancellationToken cancellationToken)
    {
        if (descriptor.Parent is null) return;

        foreach (var accessor in descriptor.ParentAccessors)
        {
            var parentType = accessor.ParentType;
            var parentInstance = Bind(parseResult, parentType, serviceResolver, returnEmpty: false, cancellationToken);
            accessor.Setter(instance, parentInstance);
        }
    }

    internal CommandRuntimeContext CreateRuntimeContext(
        ParseResult parseResult,
        ICommandServiceResolver? serviceResolver)
    {
        var functionResolver = GetInvocationFunctionResolver(serviceResolver)
            ?? CreateFunctionResolver(serviceResolver, overrideResolver: null);
        return new CommandRuntimeContext(this, parseResult, Settings, RootNode, functionResolver);
    }

    internal bool TryResolveParentInstance(
        ParseResult parseResult,
        Type definitionType,
        Type parameterType,
        ICommandServiceResolver? serviceResolver,
        CancellationToken cancellationToken,
        out object? parentInstance)
    {
        parentInstance = null;
        if (_descriptors is null) return false;

        var map = _descriptors;
        if (!map.TryGetValue(definitionType, out var node)) return false;

        var current = node.Parent;
        while (current is not null)
        {
            if (current.DefinitionType == parameterType)
            {
                parentInstance = Bind(
                    parseResult,
                    parameterType,
                    serviceResolver,
                    returnEmpty: false,
                    cancellationToken);
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    private bool IsInCalledHierarchy(ParseResult parseResult, RuntimeNode descriptor)
    {
        if (!TryGetCalledNode(parseResult, out var calledNode) || calledNode is null) return false;

        var current = calledNode;
        while (current is not null)
        {
            if (current.DefinitionType == descriptor.DefinitionType) return true;
            current = current.Parent;
        }

        return false;
    }

    private Dictionary<Type, RuntimeNode> GetDescriptorMap()
    {
        return _descriptors ?? throw new InvalidOperationException("Binding context is not initialized.");
    }

    private static Dictionary<Type, RuntimeNode> BuildDescriptorMap(RuntimeNode rootNode)
    {
        var map = new Dictionary<Type, RuntimeNode>();
        var stack = new Stack<RuntimeNode>();
        stack.Push(rootNode);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current.DefinitionType is not null) map[current.DefinitionType] = current;
            foreach (var child in current.Children)
                stack.Push(child);
        }

        return map;
    }

    private static void BuildCommandMap(RuntimeNode rootNode, Dictionary<Command, RuntimeNode> map)
    {
        var stack = new Stack<RuntimeNode>();
        stack.Push(rootNode);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            map[current.Command] = current;
            foreach (var child in current.Children)
                stack.Push(child);
        }
    }

    internal ICommandFunctionResolver? CreateFunctionResolver(
        ICommandServiceResolver? serviceResolver,
        ICommandFunctionResolver? overrideResolver)
    {
        var resolvers = new List<ICommandFunctionResolver>();
        if (overrideResolver is not null) resolvers.Add(overrideResolver);
        if (DefaultFunctionResolver is not null) resolvers.Add(DefaultFunctionResolver);
        if (AllowFunctionResolutionFromServices && serviceResolver is not null)
            resolvers.Add(new ServiceFunctionResolver(serviceResolver));

        return FunctionResolverChain.Create(resolvers);
    }

    internal ICommandFunctionResolver? GetInvocationFunctionResolver(ICommandServiceResolver? serviceResolver)
    {
        return serviceResolver is IFunctionResolverAccessor accessor ? accessor.FunctionResolver : null;
    }

    internal bool TryResolveFunctionInstance<TFunction>(ICommandFunctionResolver? functionResolver, out object? value)
    {
        value = null;
        if (!typeof(Delegate).IsAssignableFrom(typeof(TFunction)) || typeof(TFunction) == typeof(Delegate))
            return false;

        if (functionResolver is null) return false;
        if (!functionResolver.TryResolve<TFunction>(out var instance) || instance is null) return false;
        value = instance;
        return true;
    }

    internal bool TryResolveFunctionInstance(
        IFunctionTypeShape functionShape,
        ICommandFunctionResolver? functionResolver,
        out object? value)
    {
        value = null;
        if (functionResolver is null) return false;

        var result = FunctionResolveInvoker.Instance.Resolve(functionShape, functionResolver);
        if (!result.Success || result.Value is null) return false;
        if (!functionShape.Type.IsInstanceOfType(result.Value)) return false;

        value = result.Value;
        return true;
    }
}