namespace Tenekon.CommandLine.Extensions.PolyType.Runtime;

/// <summary>
/// Provides localizable strings for user-facing system messages.
/// </summary>
internal static class LocalizationResources
{
    internal static string ValueDoesNotMatchPattern(string value, string pattern)
    {
        return GetResourceString(Properties.Resources.ValueDoesNotMatchPattern, value, pattern);
    }

    internal static string FileDoesNotExist(string path)
    {
        return GetResourceString(Properties.Resources.FileDoesNotExist, path);
    }

    internal static string FileMustNotExist(string path)
    {
        return GetResourceString(Properties.Resources.FileMustNotExist, path);
    }

    internal static string DirectoryDoesNotExist(string path)
    {
        return GetResourceString(Properties.Resources.DirectoryDoesNotExist, path);
    }

    internal static string DirectoryMustNotExist(string path)
    {
        return GetResourceString(Properties.Resources.DirectoryMustNotExist, path);
    }

    internal static string FileOrDirectoryDoesNotExist(string path)
    {
        return GetResourceString(Properties.Resources.FileOrDirectoryDoesNotExist, path);
    }

    internal static string FileOrDirectoryMustNotExist(string path)
    {
        return GetResourceString(Properties.Resources.FileOrDirectoryMustNotExist, path);
    }

    internal static string InvalidPath(string path)
    {
        return GetResourceString(Properties.Resources.InvalidPath, path);
    }

    internal static string InvalidFileName(string fileName)
    {
        return GetResourceString(Properties.Resources.InvalidFileName, fileName);
    }

    internal static string InvalidUri(string uri)
    {
        return GetResourceString(Properties.Resources.InvalidUri, uri);
    }

    internal static string InvalidUrl(string url)
    {
        return GetResourceString(Properties.Resources.InvalidUrl, url);
    }

    internal static string ShowValuesLineFormat(string name, string value)
    {
        return GetResourceString(Properties.Resources.ShowValuesLineFormat, name, value);
    }

    internal static string ShowValuesNull()
    {
        return GetResourceString(Properties.Resources.ShowValuesNull);
    }

    private static string GetResourceString(string? resourceString, params object[] formatArguments)
    {
        if (resourceString is null) return string.Empty;

        if (formatArguments.Length > 0) return string.Format(resourceString, formatArguments);

        return resourceString;
    }
}