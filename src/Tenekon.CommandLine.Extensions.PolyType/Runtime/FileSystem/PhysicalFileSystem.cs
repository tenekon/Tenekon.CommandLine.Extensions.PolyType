namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.FileSystem;

/// <summary>
/// File system implementation that delegates to <see cref="System.IO"/>.
/// </summary>
public sealed class PhysicalFileSystem : IFileSystem
{
    /// <summary>
    /// Gets the file operations.
    /// </summary>
    public IFileSystemFile File { get; } = new PhysicalFileSystemFile();

    /// <summary>
    /// Gets the directory operations.
    /// </summary>
    public IFileSystemDirectory Directory { get; } = new PhysicalFileSystemDirectory();

    /// <summary>
    /// Gets the path operations.
    /// </summary>
    public IFileSystemPath Path { get; } = new PhysicalFileSystemPath();

    private sealed class PhysicalFileSystemFile : IFileSystemFile
    {
        public bool FileExists(string path)
        {
            return System.IO.File.Exists(path);
        }
    }

    private sealed class PhysicalFileSystemDirectory : IFileSystemDirectory
    {
        public bool DirectoryExists(string path)
        {
            return System.IO.Directory.Exists(path);
        }
    }

    private sealed class PhysicalFileSystemPath : IFileSystemPath
    {
        public char[] GetInvalidPathChars()
        {
            return System.IO.Path.GetInvalidPathChars();
        }

        public char[] GetInvalidFileNameChars()
        {
            return System.IO.Path.GetInvalidFileNameChars();
        }
    }
}
