using System.CommandLine;
using System.Text.RegularExpressions;

namespace Tenekon.CommandLine.Extensions.PolyType;

internal static class ValidationHelper
{
    public static void Apply(Option option, ValidationRules rules, string? pattern, string? message)
    {
        if (rules != ValidationRules.None)
            option.Validators.Add(result =>
            {
                var values = result.Tokens.Select(token => token.Value).ToArray();
                foreach (var value in values)
                    if (!ValidateRules(value, rules, out var error))
                    {
                        result.AddError(error);
                        return;
                    }
            });

        if (!string.IsNullOrWhiteSpace(pattern))
        {
            var regex = new Regex(pattern, RegexOptions.Compiled);
            option.Validators.Add(result =>
            {
                var values = result.Tokens.Select(token => token.Value).ToArray();
                foreach (var value in values)
                    if (!regex.IsMatch(value))
                    {
                        result.AddError(message ?? $"Value '{value}' does not match pattern '{pattern}'.");
                        return;
                    }
            });
        }
    }

    public static void Apply(Argument argument, ValidationRules rules, string? pattern, string? message)
    {
        if (rules != ValidationRules.None)
            argument.Validators.Add(result =>
            {
                var values = result.Tokens.Select(token => token.Value).ToArray();
                foreach (var value in values)
                    if (!ValidateRules(value, rules, out var error))
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
                        result.AddError(message ?? $"Value '{value}' does not match pattern '{pattern}'.");
                        return;
                    }
            });
        }
    }

    private static bool ValidateRules(string value, ValidationRules rules, out string error)
    {
        error = string.Empty;

        if (rules.HasFlag(ValidationRules.ExistingFile))
            if (!File.Exists(value))
            {
                error = $"File does not exist: '{value}'.";
                return false;
            }

        if (rules.HasFlag(ValidationRules.NonExistingFile))
            if (File.Exists(value))
            {
                error = $"File must not exist: '{value}'.";
                return false;
            }

        if (rules.HasFlag(ValidationRules.ExistingDirectory))
            if (!Directory.Exists(value))
            {
                error = $"Directory does not exist: '{value}'.";
                return false;
            }

        if (rules.HasFlag(ValidationRules.NonExistingDirectory))
            if (Directory.Exists(value))
            {
                error = $"Directory must not exist: '{value}'.";
                return false;
            }

        if (rules.HasFlag(ValidationRules.ExistingFileOrDirectory))
            if (!File.Exists(value) && !Directory.Exists(value))
            {
                error = $"File or directory does not exist: '{value}'.";
                return false;
            }

        if (rules.HasFlag(ValidationRules.NonExistingFileOrDirectory))
            if (File.Exists(value) || Directory.Exists(value))
            {
                error = $"File or directory must not exist: '{value}'.";
                return false;
            }

        if (rules.HasFlag(ValidationRules.LegalPath))
            if (value.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                error = $"Invalid path: '{value}'.";
                return false;
            }

        if (rules.HasFlag(ValidationRules.LegalFileName))
            if (value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                error = $"Invalid file name: '{value}'.";
                return false;
            }

        if (rules.HasFlag(ValidationRules.LegalUri))
            if (!Uri.TryCreate(value, UriKind.Absolute, out _))
            {
                error = $"Invalid URI: '{value}'.";
                return false;
            }

        if (rules.HasFlag(ValidationRules.LegalUrl))
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                error = $"Invalid URL: '{value}'.";
                return false;
            }

        return true;
    }
}