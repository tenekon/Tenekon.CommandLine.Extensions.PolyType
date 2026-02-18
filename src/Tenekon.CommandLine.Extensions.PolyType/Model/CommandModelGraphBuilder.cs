using System.Reflection;
using System.Text;
using PolyType;
using PolyType.Abstractions;
using Tenekon.CommandLine.Extensions.PolyType.Spec;

namespace Tenekon.CommandLine.Extensions.PolyType.Model;

internal static class CommandModelGraphBuilder
{
    public static CommandModelGraph Build(
        IObjectTypeShape rootShape,
        ITypeShapeProvider provider,
        CommandModelBuildOptions? options)
    {
        var effectiveOptions = options ?? CommandModelBuildOptions.Default;
        var typeNodes = new Dictionary<Type, CommandObjectNode>();
        var functionNodes = new Dictionary<Type, CommandFunctionNode>();
        var rootNode = EnsureTypeNode(rootShape, provider, typeNodes, functionNodes, effectiveOptions, isRoot: true);

        return BuildCore(rootNode, provider, typeNodes, functionNodes, effectiveOptions);
    }

    public static CommandModelGraph Build(
        IFunctionTypeShape functionShape,
        ITypeShapeProvider provider,
        CommandModelBuildOptions? options)
    {
        var effectiveOptions = options ?? CommandModelBuildOptions.Default;
        var typeNodes = new Dictionary<Type, CommandObjectNode>();
        var functionNodes = new Dictionary<Type, CommandFunctionNode>();
        var rootNode = EnsureFunctionNode(
            functionShape,
            provider,
            typeNodes,
            functionNodes,
            effectiveOptions,
            isRoot: true);

        return BuildCore(rootNode, provider, typeNodes, functionNodes, effectiveOptions);
    }

    private static CommandModelGraph BuildCore(
        ICommandGraphNode rootNode,
        ITypeShapeProvider provider,
        Dictionary<Type, CommandObjectNode> typeNodes,
        Dictionary<Type, CommandFunctionNode> functionNodes,
        CommandModelBuildOptions options)
    {
        var methodNodes = new List<CommandMethodNode>();
        var processedMethods = new HashSet<Type>();
        var previousCount = -1;
        while (previousCount != typeNodes.Count + functionNodes.Count)
        {
            previousCount = typeNodes.Count + functionNodes.Count;
            LinkRelationships(provider, rootNode, typeNodes, functionNodes, options);
            BuildMethodNodes(provider, typeNodes, functionNodes, options, processedMethods, methodNodes);
        }

        ValidateNoCycles(rootNode);

        foreach (var descriptor in typeNodes.Values.ToList())
            descriptor.InitializeModel();

        return new CommandModelGraph(GetRoot(rootNode), methodNodes, functionNodes.Values.ToList());
    }

    private static void BuildMethodNodes(
        ITypeShapeProvider provider,
        Dictionary<Type, CommandObjectNode> typeNodes,
        Dictionary<Type, CommandFunctionNode> functionNodes,
        CommandModelBuildOptions options,
        HashSet<Type> processed,
        List<CommandMethodNode> methodNodes)
    {
        var queue = new Queue<CommandObjectNode>(typeNodes.Values);

        while (queue.Count > 0)
        {
            var descriptor = queue.Dequeue();
            if (!processed.Add(descriptor.DefinitionType)) continue;

            ValidateReflectionMethodSpecs(descriptor.DefinitionType);

            var methods = new List<(IMethodShape Method, CommandSpecAttribute Spec)>();
            foreach (var method in descriptor.Shape.Methods)
            {
                var spec = method.AttributeProvider.GetCustomAttribute<CommandSpecAttribute>();
                if (spec is null) continue;

                if (method.DeclaringType.Type.IsInterface)
                    throw new InvalidOperationException(
                        $"Command method '{method.DeclaringType.Type.FullName}.{method.Name}' cannot be declared on an interface.");

                if (method.DeclaringType.Type != descriptor.DefinitionType) continue;

                methods.Add((method, spec));
            }

            if (methods.Count == 0) continue;
            ValidateMethodOverloads(descriptor.DefinitionType, methods);

            foreach (var (method, spec) in methods)
            {
                ValidateMethodNode(descriptor.DefinitionType, method, spec);
                var parameterSpecs = CollectParameterSpecs(method.Parameters);
                var methodNode = new CommandMethodNode(descriptor, method, spec, parameterSpecs);
                descriptor.MethodChildren.Add(methodNode);
                methodNodes.Add(methodNode);
            }

            foreach (var methodNode in descriptor.MethodChildren)
            {
                var children = methodNode.Spec.Children;
                if (children is null) continue;

                foreach (var childType in children)
                {
                    if (childType is null) continue;
                    var childNode = EnsureNodeForType(
                        childType,
                        provider,
                        typeNodes,
                        functionNodes,
                        options,
                        isRoot: false);

                    SetParent(childNode, methodNode, childType);

                    if (childNode is CommandObjectNode childDescriptor
                        && !processed.Contains(childDescriptor.DefinitionType))
                        queue.Enqueue(childDescriptor);
                }
            }
        }
    }

    private static void LinkRelationships(
        ITypeShapeProvider provider,
        ICommandGraphNode rootNode,
        Dictionary<Type, CommandObjectNode> typeNodes,
        Dictionary<Type, CommandFunctionNode> functionNodes,
        CommandModelBuildOptions options)
    {
        foreach (var descriptor in typeNodes.Values)
        {
            var declaringType = descriptor.DefinitionType.DeclaringType;
            if (descriptor.Parent is null && declaringType is not null
                && typeNodes.TryGetValue(declaringType, out var nestedParent))
                SetParent(descriptor, nestedParent, descriptor.DefinitionType);

            ApplySpecRelationships(descriptor, rootNode, provider, typeNodes, functionNodes, options);
        }

        foreach (var functionNode in functionNodes.Values.ToList())
            ApplySpecRelationships(functionNode, rootNode, provider, typeNodes, functionNodes, options);
    }

    private static void SetParent(ICommandGraphNode child, ICommandGraphNode parent, Type childType)
    {
        if (child.Parent is not null && !ReferenceEquals(child.Parent, parent))
            throw new InvalidOperationException($"Command '{childType.FullName}' has conflicting parents.");

        child.Parent = parent;
        if (!parent.Children.Contains(child)) parent.Children.Add(child);
    }

    private static void ValidateNoCycles(ICommandGraphNode rootNode)
    {
        var visited = new HashSet<ICommandGraphNode>();
        var stack = new HashSet<ICommandGraphNode>();

        void Visit(ICommandGraphNode node)
        {
            if (stack.Contains(node))
                throw new InvalidOperationException($"Command hierarchy has a cycle at '{node.DisplayName}'.");

            if (!visited.Add(node)) return;
            stack.Add(node);

            foreach (var child in node.Children)
                Visit(child);

            if (node is CommandObjectNode modelNode)
                foreach (var method in modelNode.MethodChildren)
                    Visit(method);

            stack.Remove(node);
        }

        Visit(GetRoot(rootNode));
    }

    private static ICommandGraphNode GetRoot(ICommandGraphNode node)
    {
        var current = node;
        var visited = new HashSet<ICommandGraphNode>();
        while (current.Parent is not null)
        {
            if (!visited.Add(current))
                throw new InvalidOperationException($"Command hierarchy has a cycle at '{current.DisplayName}'.");
            current = current.Parent;
        }
        return current;
    }

    private static CommandObjectNode EnsureTypeNode(
        IObjectTypeShape shape,
        ITypeShapeProvider provider,
        Dictionary<Type, CommandObjectNode> typeNodes,
        Dictionary<Type, CommandFunctionNode> functionNodes,
        CommandModelBuildOptions options,
        bool isRoot)
    {
        var type = shape.Type;
        if (typeNodes.TryGetValue(type, out var existing)) return existing;

        var spec = shape.AttributeProvider.GetCustomAttribute<CommandSpecAttribute>();
        if (spec is null) throw new InvalidOperationException($"Type '{type.FullName}' is missing [CommandSpec].");

        if (isRoot && spec.Parent is not null && options.RootParentHandling == RootParentHandling.Throw)
            throw new InvalidOperationException($"Root command '{type.FullName}' cannot have a parent.");

        var descriptor = new CommandObjectNode(type, shape, spec);
        typeNodes[type] = descriptor;

        EnsureSpecNodes(spec, provider, typeNodes, functionNodes, options, isRoot);

        foreach (var nested in type.GetNestedTypes())
        {
            var nestedShape = provider.GetTypeShape(nested) as IObjectTypeShape;
            if (nestedShape is null) continue;

            var nestedSpec = nestedShape.AttributeProvider.GetCustomAttribute<CommandSpecAttribute>();
            if (nestedSpec is null) continue;

            EnsureTypeNode(nestedShape, provider, typeNodes, functionNodes, options, isRoot: false);
        }

        return descriptor;
    }

    private static CommandFunctionNode EnsureFunctionNode(
        IFunctionTypeShape functionShape,
        ITypeShapeProvider provider,
        Dictionary<Type, CommandObjectNode> typeNodes,
        Dictionary<Type, CommandFunctionNode> functionNodes,
        CommandModelBuildOptions options,
        bool isRoot)
    {
        var type = functionShape.Type;
        if (functionNodes.TryGetValue(type, out var existing)) return existing;

        var spec = functionShape.AttributeProvider.GetCustomAttribute<CommandSpecAttribute>();
        if (spec is null) throw new InvalidOperationException($"Type '{type.FullName}' is missing [CommandSpec].");

        if (type.IsGenericType || type.ContainsGenericParameters || IsGenericDeclaringType(type))
            throw new InvalidOperationException($"Function '{type.FullName}' cannot be generic.");

        if (isRoot && spec.Parent is not null && options.RootParentHandling == RootParentHandling.Throw)
            throw new InvalidOperationException($"Root command '{type.FullName}' cannot have a parent.");

        var parameterSpecs = CollectParameterSpecs(functionShape.Parameters);
        var node = new CommandFunctionNode(type, functionShape, spec, parameterSpecs);
        functionNodes[type] = node;

        EnsureSpecNodes(spec, provider, typeNodes, functionNodes, options, isRoot);

        return node;
    }

    private static void EnsureSpecNodes(
        CommandSpecAttribute spec,
        ITypeShapeProvider provider,
        Dictionary<Type, CommandObjectNode> typeNodes,
        Dictionary<Type, CommandFunctionNode> functionNodes,
        CommandModelBuildOptions options,
        bool isRoot)
    {
        if (!isRoot || options.RootParentHandling != RootParentHandling.Ignore)
            if (spec.Parent is not null)
                EnsureNodeForType(spec.Parent, provider, typeNodes, functionNodes, options, isRoot: false);

        if (spec.Children is not null)
            foreach (var childType in spec.Children)
            {
                if (childType is null) continue;
                EnsureNodeForType(childType, provider, typeNodes, functionNodes, options, isRoot: false);
            }
    }

    private static void ApplySpecRelationships(
        ICommandGraphNode node,
        ICommandGraphNode rootNode,
        ITypeShapeProvider provider,
        Dictionary<Type, CommandObjectNode> typeNodes,
        Dictionary<Type, CommandFunctionNode> functionNodes,
        CommandModelBuildOptions options)
    {
        var spec = node.Spec;
        if (spec.Parent is not null)
            if (!(ReferenceEquals(node, rootNode) && options.RootParentHandling == RootParentHandling.Ignore))
            {
                var parentNode = EnsureNodeForType(
                    spec.Parent,
                    provider,
                    typeNodes,
                    functionNodes,
                    options,
                    isRoot: false);
                SetParent(node, parentNode, node.CommandType ?? spec.Parent);
            }

        if (spec.Children is not null)
            foreach (var childType in spec.Children)
            {
                if (childType is null) continue;
                var childNode = EnsureNodeForType(
                    childType,
                    provider,
                    typeNodes,
                    functionNodes,
                    options,
                    isRoot: false);
                SetParent(childNode, node, childType);
            }
    }

    private static ICommandGraphNode EnsureNodeForType(
        Type type,
        ITypeShapeProvider provider,
        Dictionary<Type, CommandObjectNode> typeNodes,
        Dictionary<Type, CommandFunctionNode> functionNodes,
        CommandModelBuildOptions options,
        bool isRoot)
    {
        if (typeNodes.TryGetValue(type, out var existingType)) return existingType;
        if (functionNodes.TryGetValue(type, out var existingFunction)) return existingFunction;

        if (IsFunctionType(type))
        {
            var functionShape = provider.GetTypeShape(type) as IFunctionTypeShape
                ?? throw new InvalidOperationException($"Type '{type.FullName}' is not shapeable as a function.");
            return EnsureFunctionNode(functionShape, provider, typeNodes, functionNodes, options, isRoot);
        }

        var objectShape = provider.GetTypeShape(type) as IObjectTypeShape
            ?? throw new InvalidOperationException($"Type '{type.FullName}' is not shapeable.");
        return EnsureTypeNode(objectShape, provider, typeNodes, functionNodes, options, isRoot);
    }

    private static bool IsFunctionType(Type type)
    {
        return typeof(Delegate).IsAssignableFrom(type) && type != typeof(Delegate);
    }

    private static void ValidateMethodOverloads(
        Type declaringType,
        IReadOnlyList<(IMethodShape Method, CommandSpecAttribute Spec)> methods)
    {
        var byName = methods.GroupBy(entry => entry.Method.Name, StringComparer.Ordinal);
        foreach (var group in byName)
        {
            if (group.Count() > 1)
                foreach (var entry in group)
                    if (string.IsNullOrWhiteSpace(entry.Spec.Name))
                        throw new InvalidOperationException(
                            $"Command method '{declaringType.FullName}.{entry.Method.Name}' requires an explicit name.");

            var signatures = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in group)
            {
                var signature = CreateMethodSignature(entry.Method);
                if (!signatures.Add(signature))
                    throw new InvalidOperationException(
                        $"Command method '{declaringType.FullName}.{entry.Method.Name}' has a duplicate signature.");
            }
        }
    }

    private static void ValidateMethodNode(Type declaringType, IMethodShape method, CommandSpecAttribute spec)
    {
        if (method.IsStatic)
            throw new InvalidOperationException(
                $"Command method '{declaringType.FullName}.{method.Name}' cannot be static.");

        if (method.DeclaringType.Type.IsInterface)
            throw new InvalidOperationException(
                $"Command method '{declaringType.FullName}.{method.Name}' cannot be declared on an interface.");

        if (method.MethodBase?.IsGenericMethod == true)
            throw new InvalidOperationException(
                $"Command method '{declaringType.FullName}.{method.Name}' cannot be generic.");

        if (IsGenericDeclaringType(method.DeclaringType.Type))
            throw new InvalidOperationException(
                $"Command method '{declaringType.FullName}.{method.Name}' cannot be declared on a generic type.");

        if (spec.Parent is not null && spec.Parent != declaringType)
            throw new InvalidOperationException(
                $"Command method '{declaringType.FullName}.{method.Name}' can only set Parent to its declaring type.");
    }

    private static void ValidateReflectionMethodSpecs(Type declaringType)
    {
        foreach (var iface in declaringType.GetInterfaces())
        foreach (var method in iface.GetMethods())
        {
            if (method.GetCustomAttribute<CommandSpecAttribute>() is null) continue;

            throw new InvalidOperationException(
                $"Command method '{iface.FullName}.{method.Name}' cannot be declared on an interface.");
        }

        for (var current = declaringType; current is not null; current = current.BaseType)
            foreach (var method in current.GetMethods(
                         BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
                         | BindingFlags.DeclaredOnly))
            {
                if (method.GetCustomAttribute<CommandSpecAttribute>() is null) continue;

                if (method.IsGenericMethod || method.ContainsGenericParameters)
                    throw new InvalidOperationException(
                        $"Command method '{current.FullName}.{method.Name}' cannot be generic.");

                if (IsGenericDeclaringType(current))
                    throw new InvalidOperationException(
                        $"Command method '{current.FullName}.{method.Name}' cannot be declared on a generic type.");
            }
    }

    private static bool IsGenericDeclaringType(Type type)
    {
        var current = type;
        while (current is not null)
        {
            if (current.IsGenericType || current.ContainsGenericParameters) return true;
            current = current.DeclaringType;
        }

        return false;
    }

    private static string CreateMethodSignature(IMethodShape method)
    {
        var builder = new StringBuilder();
        builder.Append(method.Name);
        builder.Append(value: '(');
        var first = true;
        foreach (var parameter in method.Parameters)
        {
            if (!first) builder.Append(value: ',');
            builder.Append(parameter.ParameterType.Type.FullName ?? parameter.ParameterType.Type.Name);
            first = false;
        }

        builder.Append(value: ')');
        return builder.ToString();
    }

    private static IReadOnlyList<ParameterSpecEntry> CollectParameterSpecs(IReadOnlyList<IParameterShape> parameters)
    {
        var entries = new List<ParameterSpecEntry>();
        foreach (var parameter in parameters)
        {
            var option = parameter.AttributeProvider.GetCustomAttribute<OptionSpecAttribute>();
            var argument = parameter.AttributeProvider.GetCustomAttribute<ArgumentSpecAttribute>();
            var directive = parameter.AttributeProvider.GetCustomAttribute<DirectiveSpecAttribute>();
            if (option is null && argument is null && directive is null) continue;

            entries.Add(new ParameterSpecEntry(parameter, option, argument, directive));
        }

        return entries;
    }
}