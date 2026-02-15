namespace Tenekon.CommandLine.Extensions.PolyType.Model;

internal readonly record struct SpecMemberAccessor(string DisplayName, Func<object, object?> Getter);