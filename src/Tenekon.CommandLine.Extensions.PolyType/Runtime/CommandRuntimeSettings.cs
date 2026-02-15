using System.CommandLine.Parsing;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.FileSystem;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime;

public sealed class CommandRuntimeSettings
{
    internal static CommandRuntimeSettings Default { get; } = new();

    public bool EnableDefaultExceptionHandler { get; set; }
    public bool ShowHelpOnEmptyCommand { get; set; }
    public bool AllowFunctionResolutionFromServices { get; set; }
    public bool EnableDiagramDirective { get; set; }
    public bool EnableSuggestDirective { get; set; } = true;
    public bool EnableEnvironmentVariablesDirective { get; set; }

    public TextWriter Output { get; set; } = Console.Out;
    public TextWriter Error { get; set; } = Console.Error;
    public IFileSystem FileSystem { get; set; } = new PhysicalFileSystem();

    public IList<ICommandFunctionResolver> FunctionResolvers { get; } = new List<ICommandFunctionResolver>();

    public bool EnablePosixBundling { get; set; } = true;
    public TryReplaceToken? ResponseFileTokenReplacer { get; set; }
}