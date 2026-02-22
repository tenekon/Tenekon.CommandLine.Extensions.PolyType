using System.Collections.Immutable;
using PolyType;
using PolyType.Abstractions;
using Tenekon.CommandLine.Extensions.PolyType.Runtime;
using Tenekon.CommandLine.Extensions.PolyType.Spec;

namespace Tenekon.CommandLine.Extensions.PolyType.Model.Builder;

/// <summary>
/// Provides a mutable builder for command models.
/// </summary>
public sealed class CommandModelBuilder
{
    private readonly List<CommandModelBuilderNode> _nodes = [];
    private readonly Dictionary<Type, CommandModelBuilderNode> _nodesByType = new();

    private CommandModelBuilder()
    {
    }

    internal CommandModelBuilder(CommandModel model)
    {
        var graph = model.Graph;
        var objectNodes = new Dictionary<CommandObjectNode, CommandObjectModelBuilderNode>();
        var functionNodes = new Dictionary<CommandFunctionNode, CommandFunctionModelBuilderNode>();
        var methodNodes = new Dictionary<CommandMethodNode, CommandMethodModelBuilderNode>();

        foreach (var objectNode in graph.RootNode is CommandObjectNode rootObject
                     ? EnumerateObjectNodes(rootObject)
                     : [])
        {
            var builderNode = new CommandObjectModelBuilderNode(
                objectNode.DefinitionType,
                objectNode.Shape,
                objectNode.Spec,
                objectNode.HandlerConvention);
            foreach (var entry in objectNode.SpecEntries)
                builderNode.Members.Add(
                    new CommandMemberSpecBuilder(
                        entry.OwnerType,
                        entry.SpecProperty,
                        entry.TargetProperty,
                        entry.Option is null ? null : new OptionSpecBuilder(entry.Option),
                        entry.Argument is null ? null : new ArgumentSpecBuilder(entry.Argument),
                        entry.Directive is null ? null : new DirectiveSpecBuilder(entry.Directive)));

            objectNodes[objectNode] = builderNode;
            _nodes.Add(builderNode);
            _nodesByType[objectNode.DefinitionType] = builderNode;
        }

        foreach (var functionNode in graph.FunctionNodes)
        {
            var builderNode = new CommandFunctionModelBuilderNode(
                functionNode.FunctionType,
                functionNode.FunctionShape,
                functionNode.Spec);
            foreach (var entry in functionNode.ParameterSpecs)
                builderNode.Parameters.Add(
                    new CommandParameterSpecBuilder(
                        entry.Parameter,
                        entry.Option is null ? null : new OptionSpecBuilder(entry.Option),
                        entry.Argument is null ? null : new ArgumentSpecBuilder(entry.Argument),
                        entry.Directive is null ? null : new DirectiveSpecBuilder(entry.Directive)));

            functionNodes[functionNode] = builderNode;
            _nodes.Add(builderNode);
            _nodesByType[functionNode.FunctionType] = builderNode;
        }

        foreach (var objectNode in objectNodes.Keys)
        {
            var builderParent = objectNodes[objectNode];
            foreach (var methodNode in objectNode.MethodChildren)
            {
                var builderMethod = new CommandMethodModelBuilderNode(
                    builderParent,
                    methodNode.MethodShape,
                    methodNode.Spec);
                foreach (var entry in methodNode.ParameterSpecs)
                    builderMethod.Parameters.Add(
                        new CommandParameterSpecBuilder(
                            entry.Parameter,
                            entry.Option is null ? null : new OptionSpecBuilder(entry.Option),
                            entry.Argument is null ? null : new ArgumentSpecBuilder(entry.Argument),
                            entry.Directive is null ? null : new DirectiveSpecBuilder(entry.Directive)));

                builderParent.MethodChildrenMutable.Add(builderMethod);
                methodNodes[methodNode] = builderMethod;
                _nodes.Add(builderMethod);
            }
        }

        foreach (var kvp in objectNodes)
            LinkRelationships(kvp.Key, kvp.Value, objectNodes, functionNodes, methodNodes);

        foreach (var kvp in functionNodes)
            LinkRelationships(kvp.Key, kvp.Value, objectNodes, functionNodes, methodNodes);

        foreach (var kvp in methodNodes)
            LinkRelationships(kvp.Key, kvp.Value, objectNodes, functionNodes, methodNodes);

        Root = graph.RootNode switch
        {
            CommandObjectNode objectRoot => objectNodes[objectRoot],
            CommandFunctionNode functionRoot => functionNodes[functionRoot],
            _ => throw new InvalidOperationException("Unsupported root node.")
        };
    }

    /// <summary>
    /// Creates an empty command model builder.
    /// </summary>
    /// <returns>A new <see cref="CommandModelBuilder"/>.</returns>
    public static CommandModelBuilder CreateEmpty()
    {
        return new CommandModelBuilder();
    }

    /// <summary>
    /// Gets or sets the root node for the command model.
    /// </summary>
    public CommandModelBuilderNode? Root { get; private set; }

    /// <summary>
    /// Gets all nodes tracked by this builder.
    /// </summary>
    public IReadOnlyList<CommandModelBuilderNode> Nodes => _nodes;

    /// <summary>
    /// Finds a node by command type.
    /// </summary>
    /// <param name="commandType">The command type to locate.</param>
    /// <returns>The matching node, or <see langword="null" /> if none exists.</returns>
    public CommandModelBuilderNode? Find(Type commandType)
    {
        if (commandType is null) throw new ArgumentNullException(nameof(commandType));
        return _nodesByType.TryGetValue(commandType, out var node) ? node : null;
    }

    /// <summary>
    /// Visits all nodes using the provided visitor.
    /// </summary>
    /// <param name="visitor">The visitor to invoke.</param>
    public void Visit(ICommandModelBuilderNodeVisitor visitor)
    {
        if (visitor is null) throw new ArgumentNullException(nameof(visitor));

        if (visitor is CommandModelBuilderNodeVisitor baseVisitor)
        {
            baseVisitor.Visit(this);
            return;
        }

        var visited = new HashSet<CommandModelBuilderNode>();
        foreach (var node in _nodes)
        {
            if (!visited.Add(node)) continue;
            node.Accept(visitor);
        }
    }

    /// <summary>
    /// Adds an object command for the specified command type.
    /// </summary>
    /// <typeparam name="TCommandType">The command type.</typeparam>
    /// <param name="provider">The shape provider to use.</param>
    /// <param name="spec">Optional command spec to seed the node.</param>
    /// <returns>The created or existing object command node.</returns>
    public CommandObjectModelBuilderNode AddObjectCommand<TCommandType>(
        ITypeShapeProvider provider,
        CommandSpecModel? spec = null)
    {
        return AddObjectCommand(typeof(TCommandType), provider, spec);
    }

    /// <summary>
    /// Adds an object command for the specified command type.
    /// </summary>
    /// <param name="commandType">The command type.</param>
    /// <param name="provider">The shape provider to use.</param>
    /// <param name="spec">Optional command spec to seed the node.</param>
    /// <returns>The created or existing object command node.</returns>
    public CommandObjectModelBuilderNode AddObjectCommand(
        Type commandType,
        ITypeShapeProvider provider,
        CommandSpecModel? spec = null)
    {
        if (commandType is null) throw new ArgumentNullException(nameof(commandType));
        if (provider is null) throw new ArgumentNullException(nameof(provider));

        if (_nodesByType.TryGetValue(commandType, out var existing))
        {
            if (existing is CommandObjectModelBuilderNode objectNode) return objectNode;
            throw new InvalidOperationException($"Type '{commandType.FullName}' is already registered as a command.");
        }

        var shape = provider.GetTypeShape(commandType) as IObjectTypeShape
            ?? throw new InvalidOperationException($"Type '{commandType.FullName}' is not shapeable.");

        var resolvedSpec = ResolveCommandSpec(shape, spec);
        var node = new CommandObjectModelBuilderNode(
            commandType,
            shape,
            resolvedSpec,
            CommandHandlerConventionSpecModel.CreateDefault());

        _nodes.Add(node);
        _nodesByType[commandType] = node;
        Root ??= node;

        foreach (var entry in CommandObjectNode.CollectSpecMembers(shape))
            node.Members.Add(
                new CommandMemberSpecBuilder(
                    entry.OwnerType,
                    entry.SpecProperty,
                    entry.TargetProperty,
                    entry.Option is null ? null : new OptionSpecBuilder(entry.Option),
                    entry.Argument is null ? null : new ArgumentSpecBuilder(entry.Argument),
                    entry.Directive is null ? null : new DirectiveSpecBuilder(entry.Directive)));

        AddMethodNodes(node);
        return node;
    }

    /// <summary>
    /// Adds a function command for the specified function type.
    /// </summary>
    /// <typeparam name="TFunctionType">The function type.</typeparam>
    /// <param name="provider">The shape provider to use.</param>
    /// <param name="spec">Optional command spec to seed the node.</param>
    /// <returns>The created or existing function command node.</returns>
    public CommandFunctionModelBuilderNode AddFunctionCommand<TFunctionType>(
        ITypeShapeProvider provider,
        CommandSpecModel? spec = null)
    {
        return AddFunctionCommand(typeof(TFunctionType), provider, spec);
    }

    /// <summary>
    /// Adds a function command for the specified function type.
    /// </summary>
    /// <param name="functionType">The function type.</param>
    /// <param name="provider">The shape provider to use.</param>
    /// <param name="spec">Optional command spec to seed the node.</param>
    /// <returns>The created or existing function command node.</returns>
    public CommandFunctionModelBuilderNode AddFunctionCommand(
        Type functionType,
        ITypeShapeProvider provider,
        CommandSpecModel? spec = null)
    {
        if (functionType is null) throw new ArgumentNullException(nameof(functionType));
        if (provider is null) throw new ArgumentNullException(nameof(provider));

        if (_nodesByType.TryGetValue(functionType, out var existing))
        {
            if (existing is CommandFunctionModelBuilderNode functionNode) return functionNode;
            throw new InvalidOperationException($"Type '{functionType.FullName}' is already registered as a command.");
        }

        var shape = provider.GetTypeShape(functionType) as IFunctionTypeShape
            ?? throw new InvalidOperationException($"Type '{functionType.FullName}' is not shapeable as a function.");

        CommandModelValidation.EnsureFunctionTypeValid(functionType);

        var resolvedSpec = ResolveCommandSpec(shape, spec);
        var node = new CommandFunctionModelBuilderNode(functionType, shape, resolvedSpec);
        foreach (var parameterSpec in CollectParameterSpecBuilders(shape.Parameters))
            node.Parameters.Add(parameterSpec);

        _nodes.Add(node);
        _nodesByType[functionType] = node;
        Root ??= node;
        return node;
    }

    /// <summary>
    /// Removes a node from the builder.
    /// </summary>
    /// <param name="node">The node to remove.</param>
    public void RemoveNode(CommandModelBuilderNode node)
    {
        if (node is null) throw new ArgumentNullException(nameof(node));
        if (node is CommandMethodModelBuilderNode)
            throw new InvalidOperationException("Command method nodes cannot be removed.");

        if (!_nodes.Remove(node)) return;

        if (ReferenceEquals(Root, node))
            Root = null;

        if (node.Parent is not null)
            node.Parent.ChildrenMutable.Remove(node);

        node.Parent = null;

        foreach (var child in node.ChildrenMutable.ToList()) child.Parent = null;
        node.ChildrenMutable.Clear();

        if (node is CommandObjectModelBuilderNode objectNode)
        {
            foreach (var methodNode in objectNode.MethodChildrenMutable.ToList())
            {
                foreach (var child in methodNode.Children.ToList()) child.Parent = null;

                _nodes.Remove(methodNode);
            }
            objectNode.MethodChildrenMutable.Clear();
            _nodesByType.Remove(objectNode.DefinitionType);
        }
        else if (node is CommandFunctionModelBuilderNode functionNode)
        {
            _nodesByType.Remove(functionNode.FunctionType);
        }
    }

    /// <summary>
    /// Sets the root node for the command model.
    /// </summary>
    /// <param name="root">The node to set as root.</param>
    public void SetRoot(CommandModelBuilderNode root)
    {
        if (root is null) throw new ArgumentNullException(nameof(root));
        if (root is CommandMethodModelBuilderNode)
            throw new InvalidOperationException("Command method nodes cannot be root.");
        if (!_nodes.Contains(root))
            throw new InvalidOperationException("Root node must belong to this builder.");

        Root = root;
    }

    /// <summary>
    /// Adds a child relationship between nodes.
    /// </summary>
    /// <param name="parent">The parent node.</param>
    /// <param name="child">The child node.</param>
    public void AddChild(CommandModelBuilderNode parent, CommandModelBuilderNode child)
    {
        if (parent is null) throw new ArgumentNullException(nameof(parent));
        if (child is null) throw new ArgumentNullException(nameof(child));

        SetParent(child, parent);
    }

    /// <summary>
    /// Removes a child relationship between nodes.
    /// </summary>
    /// <param name="parent">The parent node.</param>
    /// <param name="child">The child node.</param>
    public void RemoveChild(CommandModelBuilderNode parent, CommandModelBuilderNode child)
    {
        if (parent is null) throw new ArgumentNullException(nameof(parent));
        if (child is null) throw new ArgumentNullException(nameof(child));
        if (child is CommandMethodModelBuilderNode)
            throw new InvalidOperationException("Command method nodes cannot be removed from their parent.");
        if (child.Parent != parent) return;

        parent.ChildrenMutable.Remove(child);
        child.Parent = null;
    }

    /// <summary>
    /// Sets the parent of the specified node.
    /// </summary>
    /// <param name="child">The child node.</param>
    /// <param name="parent">The parent node, or <see langword="null" /> to clear.</param>
    public void SetParent(CommandModelBuilderNode child, CommandModelBuilderNode? parent)
    {
        if (child is null) throw new ArgumentNullException(nameof(child));
        if (child is CommandMethodModelBuilderNode methodNode && parent is not null
            && !ReferenceEquals(parent, methodNode.ParentType))
            throw new InvalidOperationException("Command method nodes cannot be reparented.");

        if (child.Parent is not null) child.Parent.ChildrenMutable.Remove(child);

        child.Parent = parent;
        parent?.ChildrenMutable.Add(child);
    }

    /// <summary>
    /// Validates the current builder graph and returns diagnostics.
    /// </summary>
    /// <returns>A validation result with diagnostics.</returns>
    public CommandModelValidationResult Validate()
    {
        var diagnostics = new List<CommandModelDiagnostic>();
        if (Root is null)
        {
            diagnostics.Add(new CommandModelDiagnostic("TCLM0000", "Command model root is not set."));
        }
        else
        {
            if (!_nodes.Contains(Root))
                diagnostics.Add(
                    new CommandModelDiagnostic(
                        "TCLM0003",
                        "Command model root does not belong to this builder.",
                        Node: Root));
            try
            {
                ValidateNoCycles(Root);
            }
            catch (InvalidOperationException ex)
            {
                diagnostics.Add(new CommandModelDiagnostic("TCLM0001", ex.Message, Node: Root));
            }

            var reachable = CollectReachableNodes(Root);
            foreach (var node in _nodes)
                if (!reachable.Contains(node))
                    diagnostics.Add(
                        new CommandModelDiagnostic(
                            "TCLM0002",
                            $"Command '{node.DisplayName}' is not reachable from the root.",
                            Node: node));
        }

        foreach (var node in _nodes)
            if (node is CommandFunctionModelBuilderNode functionNode)
            {
                try
                {
                    CommandModelValidation.EnsureFunctionTypeValid(functionNode.FunctionType);
                }
                catch (InvalidOperationException ex)
                {
                    diagnostics.Add(new CommandModelDiagnostic("TCLM0100", ex.Message, Node: functionNode));
                }

                ValidateParameterSpecs(
                    functionNode.FunctionShape.Parameters,
                    functionNode.Parameters,
                    functionNode,
                    diagnostics);
            }
            else if (node is CommandObjectModelBuilderNode objectNode)
            {
                ValidateMemberSpecs(objectNode, diagnostics);

                if (!objectNode.HandlerConvention.Disabled && objectNode.HandlerConvention.MethodNames.IsDefaultOrEmpty)
                    diagnostics.Add(
                        new CommandModelDiagnostic(
                            "TCLM0110",
                            $"Command '{objectNode.DisplayName}' has an empty handler convention.",
                            Node: objectNode));

                var methodEntries = objectNode.MethodChildren
                    .Select(methodNode => (methodNode, spec: methodNode.Spec.Build()))
                    .ToList();
                if (methodEntries.Count > 0)
                    try
                    {
                        CommandModelValidation.EnsureMethodOverloadsValid(
                            objectNode.DefinitionType,
                            methodEntries.Select(entry => (entry.methodNode.MethodShape, entry.spec)).ToList());
                    }
                    catch (InvalidOperationException ex)
                    {
                        diagnostics.Add(new CommandModelDiagnostic("TCLM0120", ex.Message, Node: objectNode));
                    }

                foreach (var entry in methodEntries)
                {
                    var methodNode = entry.methodNode;
                    var spec = entry.spec;
                    if (!ReferenceEquals(methodNode.Parent, methodNode.ParentType))
                        diagnostics.Add(
                            new CommandModelDiagnostic(
                                "TCLM0121",
                                $"Command method '{methodNode.DisplayName}' must use its parent type as parent.",
                                Node: methodNode));

                    try
                    {
                        CommandModelValidation.EnsureMethodNodeValid(
                            objectNode.DefinitionType,
                            methodNode.MethodShape,
                            spec);
                    }
                    catch (InvalidOperationException ex)
                    {
                        diagnostics.Add(new CommandModelDiagnostic("TCLM0122", ex.Message, Node: methodNode));
                    }

                    ValidateParameterSpecs(
                        methodNode.MethodShape.Parameters,
                        methodNode.Parameters,
                        methodNode,
                        diagnostics);
                }
            }

        return new CommandModelValidationResult(diagnostics);
    }

    /// <summary>
    /// Builds an immutable <see cref="CommandModel"/> from the current builder state.
    /// </summary>
    /// <returns>The built command model.</returns>
    public CommandModel Build()
    {
        var validation = Validate();
        if (!validation.IsValid)
            throw new CommandModelValidationException(validation.Diagnostics);
        if (Root is null)
            throw new CommandModelValidationException(
            [
                new CommandModelDiagnostic("TCLM0000", "Command model root is not set.")
            ]);

        var objectNodes = new Dictionary<CommandObjectModelBuilderNode, CommandObjectNode>();
        var functionNodes = new Dictionary<CommandFunctionModelBuilderNode, CommandFunctionNode>();
        var methodNodes = new Dictionary<CommandMethodModelBuilderNode, CommandMethodNode>();

        foreach (var node in _nodes)
            if (node is CommandObjectModelBuilderNode objectNode)
            {
                var spec = objectNode.Spec.Build();
                var handlerConvention = objectNode.HandlerConvention.Build();
                var modelNode = new CommandObjectNode(
                    objectNode.DefinitionType,
                    objectNode.Shape,
                    spec,
                    handlerConvention);

                var specEntries = objectNode.Members.Select(member => new CommandObjectNode.SpecEntry(
                        member.OwnerType,
                        member.SpecProperty,
                        member.TargetProperty,
                        member.Option?.Build(),
                        member.Argument?.Build(),
                        member.Directive?.Build()))
                    .ToList();

                modelNode.SetSpecEntries(specEntries);
                objectNodes[objectNode] = modelNode;
            }
            else if (node is CommandFunctionModelBuilderNode functionNode)
            {
                var spec = functionNode.Spec.Build();
                var parameterSpecs = functionNode.Parameters.Select(entry => new ParameterSpecEntry(
                        entry.Parameter,
                        entry.Option?.Build(),
                        entry.Argument?.Build(),
                        entry.Directive?.Build()))
                    .ToList();
                var modelNode = new CommandFunctionNode(
                    functionNode.FunctionType,
                    functionNode.FunctionShape,
                    spec,
                    parameterSpecs);
                functionNodes[functionNode] = modelNode;
            }
            else if (node is CommandMethodModelBuilderNode methodNode)
            {
                // Delay creation until parent object nodes exist.
                methodNodes[methodNode] = null!;
            }

        foreach (var kvp in methodNodes.ToList())
        {
            var builderMethod = kvp.Key;
            var parentNode = objectNodes[builderMethod.ParentType];
            var spec = builderMethod.Spec.Build();
            var parameterSpecs = builderMethod.Parameters.Select(entry => new ParameterSpecEntry(
                    entry.Parameter,
                    entry.Option?.Build(),
                    entry.Argument?.Build(),
                    entry.Directive?.Build()))
                .ToList();

            var modelMethod = new CommandMethodNode(parentNode, builderMethod.MethodShape, spec, parameterSpecs);
            parentNode.MethodChildren.Add(modelMethod);
            methodNodes[builderMethod] = modelMethod;
        }

        foreach (var node in _nodes)
        {
            if (node.Parent is null) continue;

            var parent = ResolveModelNode(node.Parent, objectNodes, functionNodes, methodNodes);
            var current = ResolveModelNode(node, objectNodes, functionNodes, methodNodes);
            if (current is CommandMethodNode)
            {
                if (current.Parent is not null && !ReferenceEquals(current.Parent, parent))
                    throw new InvalidOperationException($"Command '{current.DisplayName}' has conflicting parents.");
                continue;
            }
            if (ReferenceEquals(current.Parent, parent)) continue;

            if (current.Parent is not null && !ReferenceEquals(current.Parent, parent))
                throw new InvalidOperationException($"Command '{current.DisplayName}' has conflicting parents.");

            SetParent(current, parent);
        }

        foreach (var objectNode in objectNodes.Values)
            objectNode.InitializeModel();

        UpdateSpecRelationships(objectNodes.Values, functionNodes.Values, methodNodes.Values);

        var root = ResolveModelNode(Root, objectNodes, functionNodes, methodNodes);
        var graph = new CommandModelGraph(root, methodNodes.Values.ToList(), functionNodes.Values.ToList());

        return new CommandModel(graph);
    }

    private static void UpdateSpecRelationships(
        IEnumerable<CommandObjectNode> objectNodes,
        IEnumerable<CommandFunctionNode> functionNodes,
        IEnumerable<CommandMethodNode> methodNodes)
    {
        foreach (var node in objectNodes.Cast<ICommandGraphNode>().Concat(functionNodes).Concat(methodNodes))
        {
            var parentType = node.Parent?.CommandType;
            node.Spec.Parent = parentType;

            var childTypes = node.Children.Select(child => child.CommandType)
                .Where(type => type is not null)
                .Select(type => type!)
                .ToArray();

            node.Spec.Children = childTypes.Length == 0 ? ImmutableArray<Type>.Empty : [..childTypes];
        }
    }

    private static ICommandGraphNode ResolveModelNode(
        CommandModelBuilderNode builderNode,
        IReadOnlyDictionary<CommandObjectModelBuilderNode, CommandObjectNode> objectNodes,
        IReadOnlyDictionary<CommandFunctionModelBuilderNode, CommandFunctionNode> functionNodes,
        IReadOnlyDictionary<CommandMethodModelBuilderNode, CommandMethodNode> methodNodes)
    {
        if (builderNode is CommandObjectModelBuilderNode objectNode) return objectNodes[objectNode];
        if (builderNode is CommandFunctionModelBuilderNode functionNode) return functionNodes[functionNode];
        if (builderNode is CommandMethodModelBuilderNode methodNode) return methodNodes[methodNode];

        throw new InvalidOperationException("Unsupported builder node type.");
    }

    private static void LinkRelationships(
        ICommandGraphNode node,
        CommandModelBuilderNode builderNode,
        IReadOnlyDictionary<CommandObjectNode, CommandObjectModelBuilderNode> objectNodes,
        IReadOnlyDictionary<CommandFunctionNode, CommandFunctionModelBuilderNode> functionNodes,
        IReadOnlyDictionary<CommandMethodNode, CommandMethodModelBuilderNode> methodNodes)
    {
        if (node.Parent is null) return;

        CommandModelBuilderNode parent;
        if (node.Parent is CommandObjectNode objectParent)
            parent = objectNodes[objectParent];
        else if (node.Parent is CommandFunctionNode functionParent)
            parent = functionNodes[functionParent];
        else if (node.Parent is CommandMethodNode methodParent)
            parent = methodNodes[methodParent];
        else
            throw new InvalidOperationException("Unsupported parent node.");

        builderNode.Parent = parent;
        if (builderNode is not CommandMethodModelBuilderNode)
            parent.ChildrenMutable.Add(builderNode);
    }

    private static void SetParent(ICommandGraphNode child, ICommandGraphNode parent)
    {
        if (child.Parent is not null && !ReferenceEquals(child.Parent, parent))
            throw new InvalidOperationException($"Command '{child.DisplayName}' has conflicting parents.");

        SetParentInternal(child, parent);

        var parentChildren = GetChildrenMutable(parent);
        if (!parentChildren.Contains(child)) parentChildren.Add(child);
    }

    private static void SetParentInternal(ICommandGraphNode node, ICommandGraphNode? parent)
    {
        switch (node)
        {
            case CommandObjectNode objectNode:
                objectNode.Parent = parent;
                break;
            case CommandMethodNode methodNode:
                methodNode.Parent = parent;
                break;
            case CommandFunctionNode functionNode:
                functionNode.Parent = parent;
                break;
            default:
                throw new InvalidOperationException("Unsupported command node.");
        }
    }

    private static List<ICommandGraphNode> GetChildrenMutable(ICommandGraphNode node)
    {
        return node switch
        {
            CommandObjectNode objectNode => objectNode.Children,
            CommandMethodNode methodNode => methodNode.Children,
            CommandFunctionNode functionNode => functionNode.Children,
            _ => throw new InvalidOperationException("Unsupported command node.")
        };
    }

    private static IEnumerable<CommandObjectNode> EnumerateObjectNodes(CommandObjectNode root)
    {
        var stack = new Stack<CommandObjectNode>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            yield return current;

            foreach (var child in current.Children.OfType<CommandObjectNode>())
                stack.Push(child);

            foreach (var method in current.MethodChildren)
            foreach (var child in method.Children.OfType<CommandObjectNode>())
                stack.Push(child);
        }
    }

    private static void ValidateNoCycles(CommandModelBuilderNode root)
    {
        var visited = new HashSet<CommandModelBuilderNode>();
        var stack = new HashSet<CommandModelBuilderNode>();

        void Visit(CommandModelBuilderNode node)
        {
            if (stack.Contains(node))
                throw new InvalidOperationException($"Command hierarchy has a cycle at '{node.DisplayName}'.");

            if (!visited.Add(node)) return;
            stack.Add(node);

            foreach (var child in node.Children)
                Visit(child);

            if (node is CommandObjectModelBuilderNode objectNode)
                foreach (var method in objectNode.MethodChildren)
                    Visit(method);

            stack.Remove(node);
        }

        Visit(root);
    }

    private static CommandSpecModel ResolveCommandSpec(ITypeShape shape, CommandSpecModel? spec)
    {
        if (spec is not null) return spec;
        var specAttribute = shape.AttributeProvider.GetCustomAttribute<CommandSpecAttribute>();
        if (specAttribute is null)
            throw new InvalidOperationException($"Type '{shape.Type.FullName}' is missing [CommandSpec].");
        return CommandSpecModel.FromAttribute(specAttribute);
    }

    private void AddMethodNodes(CommandObjectModelBuilderNode parent)
    {
        var methods = new List<(IMethodShape Method, CommandSpecModel Spec)>();
        foreach (var method in parent.Shape.Methods)
        {
            var specAttribute = method.AttributeProvider.GetCustomAttribute<CommandSpecAttribute>();
            if (specAttribute is null) continue;
            if (method.DeclaringType.Type != parent.DefinitionType) continue;

            var spec = CommandSpecModel.FromAttribute(specAttribute);
            methods.Add((method, spec));
        }

        if (methods.Count == 0) return;

        CommandModelValidation.EnsureMethodOverloadsValid(parent.DefinitionType, methods);

        foreach (var (method, spec) in methods)
        {
            CommandModelValidation.EnsureMethodNodeValid(parent.DefinitionType, method, spec);

            var builderMethod = new CommandMethodModelBuilderNode(parent, method, spec);
            foreach (var parameterSpec in CollectParameterSpecBuilders(method.Parameters))
                builderMethod.Parameters.Add(parameterSpec);

            parent.MethodChildrenMutable.Add(builderMethod);
            _nodes.Add(builderMethod);
        }
    }

    private static IReadOnlyList<CommandParameterSpecBuilder> CollectParameterSpecBuilders(
        IReadOnlyList<IParameterShape> parameters)
    {
        var entries = new List<CommandParameterSpecBuilder>();
        foreach (var parameter in parameters)
        {
            var option = parameter.AttributeProvider.GetCustomAttribute<OptionSpecAttribute>();
            var argument = parameter.AttributeProvider.GetCustomAttribute<ArgumentSpecAttribute>();
            var directive = parameter.AttributeProvider.GetCustomAttribute<DirectiveSpecAttribute>();
            if (option is null && argument is null && directive is null) continue;

            entries.Add(
                new CommandParameterSpecBuilder(
                    parameter,
                    option is null ? null : new OptionSpecBuilder(OptionSpecModel.FromAttribute(option)),
                    argument is null ? null : new ArgumentSpecBuilder(ArgumentSpecModel.FromAttribute(argument)),
                    directive is null ? null : new DirectiveSpecBuilder(DirectiveSpecModel.FromAttribute(directive))));
        }

        return entries;
    }

    private static HashSet<CommandModelBuilderNode> CollectReachableNodes(CommandModelBuilderNode root)
    {
        var visited = new HashSet<CommandModelBuilderNode>();
        var stack = new Stack<CommandModelBuilderNode>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!visited.Add(current)) continue;

            foreach (var child in current.Children)
                stack.Push(child);

            if (current is CommandObjectModelBuilderNode objectNode)
                foreach (var method in objectNode.MethodChildren)
                    stack.Push(method);
        }

        return visited;
    }

    private static void ValidateMemberSpecs(
        CommandObjectModelBuilderNode objectNode,
        List<CommandModelDiagnostic> diagnostics)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var shapeProperties = new HashSet<string>(
            objectNode.Shape.Properties.Select(property => property.Name),
            StringComparer.Ordinal);

        foreach (var member in objectNode.Members)
        {
            if (!seen.Add(member.SpecProperty.Name))
                diagnostics.Add(
                    new CommandModelDiagnostic(
                        "TCLM0200",
                        $"Property '{member.SpecProperty.Name}' appears multiple times on '{objectNode.DisplayName}'.",
                        Node: objectNode,
                        Property: member.SpecProperty));

            if (member.Option is null && member.Argument is null && member.Directive is null)
                diagnostics.Add(
                    new CommandModelDiagnostic(
                        "TCLM0201",
                        $"Property '{member.SpecProperty.Name}' does not define a CLI spec.",
                        Node: objectNode,
                        Property: member.SpecProperty));

            var ownerType = member.OwnerType;
            if (ownerType != objectNode.DefinitionType && !ownerType.IsAssignableFrom(objectNode.DefinitionType))
                diagnostics.Add(
                    new CommandModelDiagnostic(
                        "TCLM0202",
                        $"Property '{member.SpecProperty.Name}' has an invalid owner type '{ownerType.FullName}'.",
                        Node: objectNode,
                        Property: member.SpecProperty));

            if (!shapeProperties.Contains(member.TargetProperty.Name))
                diagnostics.Add(
                    new CommandModelDiagnostic(
                        "TCLM0203",
                        $"Target property '{member.TargetProperty.Name}' is not defined on '{objectNode.DisplayName}'.",
                        Node: objectNode,
                        Property: member.TargetProperty));

            if (!shapeProperties.Contains(member.SpecProperty.Name))
            {
                var ownerShape = objectNode.Shape.Provider.GetTypeShape(ownerType) as IObjectTypeShape;
                var ownerProperties = ownerShape is null
                    ? null
                    : new HashSet<string>(
                        ownerShape.Properties.Select(property => property.Name),
                        StringComparer.Ordinal);
                if (ownerProperties is null || !ownerProperties.Contains(member.SpecProperty.Name))
                    diagnostics.Add(
                        new CommandModelDiagnostic(
                            "TCLM0204",
                            $"Property '{member.SpecProperty.Name}' is not defined on '{objectNode.DisplayName}' or its interfaces.",
                            Node: objectNode,
                            Property: member.SpecProperty));
            }
        }
    }

    private static void ValidateParameterSpecs(
        IReadOnlyList<IParameterShape> parameters,
        IList<CommandParameterSpecBuilder> specs,
        CommandModelBuilderNode node,
        List<CommandModelDiagnostic> diagnostics)
    {
        var byPosition = new Dictionary<int, IParameterShape>();
        var parameterSet = new HashSet<IParameterShape>(parameters);

        foreach (var entry in specs)
        {
            if (!parameterSet.Contains(entry.Parameter))
            {
                diagnostics.Add(
                    new CommandModelDiagnostic(
                        "TCLM0300",
                        $"Parameter '{entry.Parameter.Name}' does not belong to '{node.DisplayName}'.",
                        Node: node,
                        Parameter: entry.Parameter));
                continue;
            }

            if (byPosition.TryGetValue(entry.Parameter.Position, out var existing))
                diagnostics.Add(
                    new CommandModelDiagnostic(
                        "TCLM0301",
                        $"Parameter '{entry.Parameter.Name}' conflicts with '{existing.Name}' on '{node.DisplayName}'.",
                        Node: node,
                        Parameter: entry.Parameter));
            else
                byPosition[entry.Parameter.Position] = entry.Parameter;

            var parameterType = entry.Parameter.ParameterType.Type;
            if (parameterType == typeof(CommandRuntimeContext) || parameterType == typeof(CancellationToken))
                diagnostics.Add(
                    new CommandModelDiagnostic(
                        "TCLM0302",
                        $"Parameter '{entry.Parameter.Name}' cannot define CLI specs.",
                        Node: node,
                        Parameter: entry.Parameter));

            if (entry.Option is null && entry.Argument is null && entry.Directive is null)
                diagnostics.Add(
                    new CommandModelDiagnostic(
                        "TCLM0303",
                        $"Parameter '{entry.Parameter.Name}' does not define a CLI spec.",
                        Node: node,
                        Parameter: entry.Parameter));
        }
    }
}
