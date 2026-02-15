using Tenekon.CommandLine.Extensions.PolyType.Runtime.FileSystem;
using Testably.Abstractions.Testing;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Fixtures;

internal sealed class MockFileSystemAdapter : IFileSystem
{
    private readonly MockFileSystem _fileSystem;

    public MockFileSystemAdapter(MockFileSystem fileSystem)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        File = new FileOutlet(_fileSystem);
        Directory = new DirectoryOutlet(_fileSystem);
        Path = new PathOutlet(_fileSystem);
    }

    public IFileSystemFile File { get; }

    public IFileSystemDirectory Directory { get; }

    public IFileSystemPath Path { get; }

    private sealed class FileOutlet(MockFileSystem fileSystem) : IFileSystemFile
    {
        public bool FileExists(string path)
        {
            return fileSystem.File.Exists(path);
        }
    }

    private sealed class DirectoryOutlet(MockFileSystem fileSystem) : IFileSystemDirectory
    {
        public bool DirectoryExists(string path)
        {
            return fileSystem.Directory.Exists(path);
        }
    }

    private sealed class PathOutlet(MockFileSystem fileSystem) : IFileSystemPath
    {
        public char[] GetInvalidPathChars()
        {
            return fileSystem.Path.GetInvalidPathChars();
        }

        public char[] GetInvalidFileNameChars()
        {
            return fileSystem.Path.GetInvalidFileNameChars();
        }
    }
}