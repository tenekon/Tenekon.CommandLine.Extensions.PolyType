namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.FileSystem;

/// <summary>
/// Provides access to file system operations.
/// </summary>
public interface IFileSystem
{
    /// <summary>
    /// Gets file operations.
    /// </summary>
    IFileSystemFile File { get; }

    /// <summary>
    /// Gets directory operations.
    /// </summary>
    IFileSystemDirectory Directory { get; }

    /// <summary>
    /// Gets path operations.
    /// </summary>
    IFileSystemPath Path { get; }
}
