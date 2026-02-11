using System.CommandLine.Parsing;

namespace Tenekon.CommandLine.Extensions.PolyType;

public sealed class CommandLineSettings
{
    internal static CommandLineSettings Default { get; } = new();
    
    public bool EnableDefaultExceptionHandler { get; set; }
    public bool ShowHelpOnEmptyCommand { get; set; } = true;
    public bool EnableDiagramDirective { get; set; }
    public bool EnableSuggestDirective { get; set; } = true;
    public bool EnableEnvironmentVariablesDirective { get; set; }

    public TextWriter Output { get; set; } = Console.Out;
    public TextWriter Error { get; set; } = Console.Error;

    public bool EnablePosixBundling { get; set; } = true;
    public TryReplaceToken? ResponseFileTokenReplacer { get; set; }
}