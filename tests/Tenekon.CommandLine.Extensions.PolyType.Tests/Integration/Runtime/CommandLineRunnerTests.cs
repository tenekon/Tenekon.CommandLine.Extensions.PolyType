using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Tests.Infrastructure;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Runtime;

public class CommandLineRunnerTests
{
    [Fact]
    public void Create_WithSettings_RunsSuccessfully()
    {
        var settings = new CommandRuntimeSettings();

        var cliApp = CommandRuntime.Factory.Object.Create<BasicRootCommand>(settings);
        cliApp.ShouldNotBeNull();

        var optionName = TestNamingPolicy.CreateDefault().GetOptionName(nameof(BasicRootCommand.Option1));

        var exitCode = cliApp.Run([optionName, "value", "argument"]);
        exitCode.ShouldBe(expected: 0);
    }
}