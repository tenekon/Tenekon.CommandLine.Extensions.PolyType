using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Execution;

public class CommandLineRunnerTests
{
    [Fact]
    public void Create_WithSettings_RunsSuccessfully()
    {
        var settings = new CommandLineSettings
        {
            ShowHelpOnEmptyCommand = false
        };
        
        var cliApp = CommandLineApp.CreateFromType<BasicRootCommand>(settings);
        cliApp.ShouldNotBeNull();
        
        var namer = new CommandLineNamer(
            nameAutoGenerate: null,
            nameCasingConvention: null,
            namePrefixConvention: null,
            shortFormAutoGenerate: null,
            shortFormPrefixConvention: null);
        
        var optionName = namer.GetOptionName(nameof(BasicRootCommand.Option1));

        var exitCode = cliApp.Run([optionName, "value", "argument"]);
        exitCode.ShouldBe(0);
    }
}