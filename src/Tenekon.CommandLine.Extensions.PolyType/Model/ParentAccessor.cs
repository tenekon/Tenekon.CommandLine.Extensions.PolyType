namespace Tenekon.CommandLine.Extensions.PolyType.Model;

internal readonly record struct ParentAccessor(Type ParentType, Action<object, object> Setter);