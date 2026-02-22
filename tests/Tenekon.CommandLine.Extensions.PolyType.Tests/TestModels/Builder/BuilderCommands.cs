using PolyType;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels.Builder;

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class BuilderMemberCommand
{
    public string? OptionValue { get; set; }
    public string? ArgumentValue { get; set; }
    public string? DirectiveValue { get; set; }

    public void Run() { }
}

internal static class BuilderMethodLog
{
    public static string? LastOption { get; set; }
    public static int LastArgument { get; set; }

    public static void Reset()
    {
        LastOption = null;
        LastArgument = 0;
    }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class BuilderMethodCommand
{
    [CommandSpec(Name = "invoke")]
    public void Invoke(string optionValue, int argumentValue)
    {
        BuilderMethodLog.LastOption = optionValue;
        BuilderMethodLog.LastArgument = argumentValue;
    }
}

[GenerateShapeFor(typeof(BuilderFunctionCommand))]
public partial class BuilderFunctionWitness;

[CommandSpec]
public delegate void BuilderFunctionCommand(string optionValue, int argumentValue);

internal static class BuilderFunctionLog
{
    public static string? LastOption { get; set; }
    public static int LastArgument { get; set; }

    public static void Reset()
    {
        LastOption = null;
        LastArgument = 0;
    }
}