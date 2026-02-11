using Shouldly;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Execution;

public class CommandLineSettingsTests
{
    [Fact]
    public void CommandLineSettings_DefaultValues_AreInitialized()
    {
        var settings = new CommandLineSettings();

        settings.ShowHelpOnEmptyCommand.ShouldBeTrue();
        settings.EnableSuggestDirective.ShouldBeTrue();
        settings.EnablePosixBundling.ShouldBeTrue();
        settings.Output.ShouldNotBeNull();
        settings.Error.ShouldNotBeNull();
    }
}