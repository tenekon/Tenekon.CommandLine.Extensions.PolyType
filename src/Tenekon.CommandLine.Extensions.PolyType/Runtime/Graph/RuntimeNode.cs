using System.CommandLine;
using PolyType.Abstractions;
using Tenekon.CommandLine.Extensions.PolyType.Model;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.Graph;

internal enum RuntimeNodeKind
{
    Type,
    Method,
    Function
}

internal sealed class RuntimeNode
{
    private RuntimeNode(
        RuntimeNodeKind kind,
        Type? definitionType,
        IMethodShape? methodShape,
        IFunctionTypeShape? functionShape,
        Command command,
        IReadOnlyList<RuntimeValueAccessor> valueAccessors,
        IReadOnlyList<ParentAccessor> parentAccessors)
    {
        Kind = kind;
        DefinitionType = definitionType;
        MethodShape = methodShape;
        FunctionShape = functionShape;
        Command = command;
        ValueAccessors = valueAccessors;
        ParentAccessors = parentAccessors;
    }

    public static RuntimeNode CreateType(
        Type definitionType,
        Command command,
        IReadOnlyList<RuntimeValueAccessor> valueAccessors,
        IReadOnlyList<ParentAccessor> parentAccessors)
    {
        return new RuntimeNode(
            RuntimeNodeKind.Type,
            definitionType,
            methodShape: null,
            functionShape: null,
            command,
            valueAccessors,
            parentAccessors);
    }

    public static RuntimeNode CreateMethod(
        IMethodShape methodShape,
        Command command,
        IReadOnlyList<RuntimeValueAccessor> valueAccessors)
    {
        return new RuntimeNode(
            RuntimeNodeKind.Method,
            definitionType: null,
            methodShape,
            functionShape: null,
            command,
            valueAccessors,
            []);
    }

    public static RuntimeNode CreateFunction(
        Type functionType,
        IFunctionTypeShape functionShape,
        Command command,
        IReadOnlyList<RuntimeValueAccessor> valueAccessors)
    {
        return new RuntimeNode(
            RuntimeNodeKind.Function,
            functionType,
            methodShape: null,
            functionShape,
            command,
            valueAccessors,
            []);
    }

    public RuntimeNodeKind Kind { get; }
    public Type? DefinitionType { get; }
    public IMethodShape? MethodShape { get; }
    public IFunctionTypeShape? FunctionShape { get; }
    public Command Command { get; internal set; }
    public RuntimeNode? Parent { get; internal set; }
    public List<RuntimeNode> Children { get; } = [];
    public IReadOnlyList<RuntimeValueAccessor> ValueAccessors { get; }
    public IReadOnlyList<ParentAccessor> ParentAccessors { get; }

    public string DisplayName => Command.Name ?? DefinitionType?.Name ?? MethodShape?.Name ?? string.Empty;

    public RuntimeNode GetRoot()
    {
        var current = this;
        while (current.Parent is not null)
            current = current.Parent;
        return current;
    }

    public RuntimeNode? Find(Type type)
    {
        if (DefinitionType == type) return this;
        foreach (var child in Children)
        {
            var found = child.Find(type);
            if (found is not null) return found;
        }

        return null;
    }
}