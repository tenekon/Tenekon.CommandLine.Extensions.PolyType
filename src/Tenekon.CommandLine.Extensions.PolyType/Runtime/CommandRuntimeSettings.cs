using System.CommandLine.Parsing;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.FileSystem;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime;

/// <summary>
/// Configures runtime parsing, output, and resolution behavior.
/// </summary>
public sealed class CommandRuntimeSettings
{
    internal static CommandRuntimeSettings Default { get; } = new();

    /// <summary>
    /// Gets or sets whether the built-in help option is enabled.
    /// </summary>
    public bool EnableHelpOption { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the built-in version option is enabled.
    /// </summary>
    public bool EnableVersionOption { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the default exception handler is enabled.
    /// </summary>
    public bool EnableDefaultExceptionHandler { get; set; }

    /// <summary>
    /// Gets or sets whether help is shown when no command is provided.
    /// </summary>
    public bool ShowHelpOnEmptyCommand { get; set; }

    /// <summary>
    /// Gets or sets whether function resolution can use services.
    /// </summary>
    public bool AllowFunctionResolutionFromServices { get; set; }

    /// <summary>
    /// Gets or sets whether the diagram directive is enabled.
    /// </summary>
    public bool EnableDiagramDirective { get; set; }

    /// <summary>
    /// Gets or sets whether the suggest directive is enabled.
    /// </summary>
    public bool EnableSuggestDirective { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the environment variables directive is enabled.
    /// </summary>
    public bool EnableEnvironmentVariablesDirective { get; set; }

    /// <summary>
    /// Gets or sets the output writer.
    /// </summary>
    public TextWriter Output { get; set; } = Console.Out;

    /// <summary>
    /// Gets or sets the error writer.
    /// </summary>
    public TextWriter Error { get; set; } = Console.Error;

    /// <summary>
    /// Gets or sets the file system abstraction used for validation.
    /// </summary>
    public IFileSystem FileSystem { get; set; } = new PhysicalFileSystem();

    /// <summary>
    /// Gets the ordered list of function resolvers.
    /// </summary>
    public IList<ICommandFunctionResolver> FunctionResolvers { get; } = new List<ICommandFunctionResolver>();

    /// <summary>
    /// Gets or sets whether POSIX-style bundling is enabled.
    /// </summary>
    public bool EnablePosixBundling { get; set; } = true;

    /// <summary>
    /// Gets or sets the response file token replacer.
    /// </summary>
    public TryReplaceToken? ResponseFileTokenReplacer { get; set; }
}
