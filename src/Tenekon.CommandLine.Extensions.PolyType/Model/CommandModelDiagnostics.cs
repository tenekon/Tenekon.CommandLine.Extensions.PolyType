using PolyType.Abstractions;
using Tenekon.CommandLine.Extensions.PolyType.Model.Builder;

namespace Tenekon.CommandLine.Extensions.PolyType.Model;

/// <summary>
/// Severity levels for command model diagnostics.
/// </summary>
public enum CommandModelDiagnosticSeverity
{
    /// <summary>Indicates an error that invalidates the model.</summary>
    Error,
    /// <summary>Indicates a non-fatal issue.</summary>
    Warning
}

/// <summary>
/// Describes a validation issue found when building a command model.
/// </summary>
public sealed record CommandModelDiagnostic(
    string Code,
    string Message,
    CommandModelDiagnosticSeverity Severity = CommandModelDiagnosticSeverity.Error,
    CommandModelBuilderNode? Node = null,
    IPropertyShape? Property = null,
    IParameterShape? Parameter = null);

/// <summary>
/// Result of validating a command model.
/// </summary>
public sealed class CommandModelValidationResult
{
    /// <summary>
    /// Creates a new validation result from the given diagnostics.
    /// </summary>
    /// <param name="diagnostics">Diagnostics produced during validation.</param>
    public CommandModelValidationResult(IReadOnlyList<CommandModelDiagnostic> diagnostics)
    {
        Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
    }

    /// <summary>Gets the validation diagnostics.</summary>
    public IReadOnlyList<CommandModelDiagnostic> Diagnostics { get; }

    /// <summary>Returns true when there are no error diagnostics.</summary>
    public bool IsValid => Diagnostics.All(diagnostic => diagnostic.Severity != CommandModelDiagnosticSeverity.Error);
}

/// <summary>
/// Exception thrown when command model validation fails.
/// </summary>
public sealed class CommandModelValidationException : InvalidOperationException
{
    /// <summary>
    /// Creates an exception from the given diagnostics.
    /// </summary>
    /// <param name="diagnostics">Diagnostics produced during validation.</param>
    public CommandModelValidationException(IReadOnlyList<CommandModelDiagnostic> diagnostics) : base(
        BuildMessage(diagnostics))
    {
        Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
    }

    /// <summary>Gets the validation diagnostics.</summary>
    public IReadOnlyList<CommandModelDiagnostic> Diagnostics { get; }

    private static string BuildMessage(IReadOnlyList<CommandModelDiagnostic>? diagnostics)
    {
        if (diagnostics is null || diagnostics.Count == 0) return "Command model validation failed.";

        return $"Command model validation failed: {diagnostics[index: 0].Message}";
    }
}
