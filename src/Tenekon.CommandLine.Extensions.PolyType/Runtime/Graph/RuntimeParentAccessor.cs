namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.Graph;

internal readonly record struct RuntimeParentAccessor(Type ParentType, Action<object, object> Setter);