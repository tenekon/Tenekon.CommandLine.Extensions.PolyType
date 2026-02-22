namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.FileSystem;

/// <summary>
/// Provides path-related operations.
/// </summary>
public interface IFileSystemPath
{
    /// <summary>
    /// Gets the invalid characters for paths.
    /// </summary>
    /// <returns>An array of invalid path characters.</returns>
    char[] GetInvalidPathChars();

    /// <summary>
    /// Gets the invalid characters for file names.
    /// </summary>
    /// <returns>An array of invalid file name characters.</returns>
    char[] GetInvalidFileNameChars();
}
