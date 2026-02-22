namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.FileSystem;

/// <summary>
/// Provides directory-related operations.
/// </summary>
public interface IFileSystemDirectory
{
    /// <summary>
    /// Determines whether a directory exists at the specified path.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <returns><see langword="true" /> if the directory exists; otherwise <see langword="false" />.</returns>
    bool DirectoryExists(string path);
}
