namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.Binding;

internal readonly record struct BinderKey(Type CommandType, Type TargetType);