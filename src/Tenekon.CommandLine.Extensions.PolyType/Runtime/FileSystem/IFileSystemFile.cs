namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.FileSystem;

/// <summary>
/// Provides file-related operations.
/// </summary>
public interface IFileSystemFile
{
    /// <summary>
    /// Determines whether a file exists at the specified path.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns><see langword="true" /> if the file exists; otherwise <see langword="false" />.</returns>
    bool FileExists(string path);
}
