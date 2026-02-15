namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.FileSystem;

public sealed class PhysicalFileSystem : IFileSystem
{
    public IFileSystemFile File { get; } = new PhysicalFileSystemFile();

    public IFileSystemDirectory Directory { get; } = new PhysicalFileSystemDirectory();

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