using Microsoft.Extensions.DependencyInjection;
using PolyType;
using Tenekon.CommandLine.Extensions.PolyType.Tests.Fixtures;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

public interface IDescribingCommand
{
}

public interface IChainableCommand
{
    string[]? ForwardedArguments { get; set; }
}

internal static class HelperLog
{
    public static int RootRuns { get; set; }
    public static int NextRuns { get; set; }
    public static string? ConfiguredValue { get; set; }
    public static int DescribingRuns { get; set; }

    public static void Reset()
    {
        RootRuns = 0;
        NextRuns = 0;
        ConfiguredValue = null;
        DescribingRuns = 0;
    }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class HelperRootCommand : IChainableCommand
{
    [OptionSpec(Name = "forward")]
    public bool Forward { get; set; }

    public string[]? ForwardedArguments { get; set; }

    public Task RunAsync()
    {
        HelperLog.RootRuns++;
        if (Forward) ForwardedArguments = ["next"];

        return Task.CompletedTask;
    }

    [CommandSpec]
    [GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
    public partial class NextCommand
    {
        public void Run()
        {
            HelperLog.NextRuns++;
        }
    }
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class HelperDescribingCommand : IDescribingCommand
{
    public void Run()
    {
        HelperLog.DescribingRuns++;
    }
}

public sealed class ConfiguredService(string value)
{
    public string Value { get; } = value;
}

[CommandSpec]
[GenerateShape(IncludeMethods = MethodShapeFlags.AllPublic)]
public partial class ConfigurableCommand
{
    [OptionSpec(Name = "name")]
    public string Name { get; set; } = "";

    public static string? BoundValue { get; set; }

    public static void ConfigureCommand(ConfigureCommandContext context)
    {
        var instance = new ConfigurableCommand();
        context.BindCommandProperties(typeof(ConfigurableCommand), instance);
        BoundValue = instance.Name;
        context.Services.AddSingleton(new ConfiguredService(instance.Name));
    }

    public void Run(ConfiguredService service)
    {
        HelperLog.ConfiguredValue = service.Value;
    }
}