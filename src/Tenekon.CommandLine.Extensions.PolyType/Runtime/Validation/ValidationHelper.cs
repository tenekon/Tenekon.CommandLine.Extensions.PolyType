using System.CommandLine;
using System.Text.RegularExpressions;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.FileSystem;
using Tenekon.CommandLine.Extensions.PolyType.Spec;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.Validation;

internal static class ValidationHelper
{
    public static void Apply(
        Option option,
        ValidationRules rules,
        string? pattern,
        string? message,
        IFileSystem fileSystem)
    {
        if (rules != ValidationRules.None)
            option.Validators.Add(result =>
            {
                var values = result.Tokens.Select(token => token.Value).ToArray();
                foreach (var value in values)
                    if (!ValidateRules(value, rules, fileSystem, out var error))
                    {
                        result.AddError(error);
                        return;
                    }
            });

        if (!string.IsNullOrWhiteSpace(pattern))
            option.Validators.Add(result =>
            {
                var values = result.Tokens.Select(token => token.Value).ToArray();
                foreach (var value in values)
                    if (!Regex.IsMatch(value, pattern))
                    {
                        result.AddError(message ?? LocalizationResources.ValueDoesNotMatchPattern(value, pattern));
                        return;
                    }
            });
    }

    public static void Apply(
        Argument argument,
        ValidationRules rules,
        string? pattern,
        string? message,
        IFileSystem fileSystem)
    {
        if (rules != ValidationRules.None)
            argument.Validators.Add(result =>
            {
                var values = result.Tokens.Select(token => token.Value).ToArray();
                foreach (var value in values)
                    if (!ValidateRules(value, rules, fileSystem, out var error))
                    {
                        result.AddError(error);
                        return;
                    }
            });

        if (!string.IsNullOrWhiteSpace(pattern))
        {
            var regex = new Regex(pattern, RegexOptions.Compiled);
            argument.Validators.Add(result =>
            {
                var values = result.Tokens.Select(token => token.Value).ToArray();
                foreach (var value in values)
                    if (!regex.IsMatch(value))
                    {
                        result.AddError(message ?? LocalizationResources.ValueDoesNotMatchPattern(value, pattern));
                        return;
                    }
            });
        }
    }

    private static bool ValidateRules(string value, ValidationRules rules, IFileSystem fileSystem, out string error)
    {
        error = string.Empty;

        if (rules.HasFlag(ValidationRules.ExistingFile))
            if (!fileSystem.File.FileExists(value))
            {
                error = LocalizationResources.FileDoesNotExist(value);
                return false;
            }

        if (rules.HasFlag(ValidationRules.NonExistingFile))
            if (fileSystem.File.FileExists(value))
            {
                error = LocalizationResources.FileMustNotExist(value);
                return false;
            }

        if (rules.HasFlag(ValidationRules.ExistingDirectory))
            if (!fileSystem.Directory.DirectoryExists(value))
            {
                error = LocalizationResources.DirectoryDoesNotExist(value);
                return false;
            }

        if (rules.HasFlag(ValidationRules.NonExistingDirectory))
            if (fileSystem.Directory.DirectoryExists(value))
            {
                error = LocalizationResources.DirectoryMustNotExist(value);
                return false;
            }

        if (rules.HasFlag(ValidationRules.ExistingFileOrDirectory))
            if (!fileSystem.File.FileExists(value) && !fileSystem.Directory.DirectoryExists(value))
            {
                error = LocalizationResources.FileOrDirectoryDoesNotExist(value);
                return false;
            }

        if (rules.HasFlag(ValidationRules.NonExistingFileOrDirectory))
            if (fileSystem.File.FileExists(value) || fileSystem.Directory.DirectoryExists(value))
            {
                error = LocalizationResources.FileOrDirectoryMustNotExist(value);
                return false;
            }

        if (rules.HasFlag(ValidationRules.LegalPath))
            if (value.IndexOfAny(fileSystem.Path.GetInvalidPathChars()) >= 0)
            {
                error = LocalizationResources.InvalidPath(value);
                return false;
            }

        if (rules.HasFlag(ValidationRules.LegalFileName))
            if (value.IndexOfAny(fileSystem.Path.GetInvalidFileNameChars()) >= 0)
            {
                error = LocalizationResources.InvalidFileName(value);
                return false;
            }

        if (rules.HasFlag(ValidationRules.LegalUri))
            if (!Uri.TryCreate(value, UriKind.Absolute, out _))
            {
                error = LocalizationResources.InvalidUri(value);
                return false;
            }

        if (rules.HasFlag(ValidationRules.LegalUrl))
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                error = LocalizationResources.InvalidUrl(value);
                return false;
            }

        return true;
    }
}
