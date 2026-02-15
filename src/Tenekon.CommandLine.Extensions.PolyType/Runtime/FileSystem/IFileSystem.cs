namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.FileSystem;

public interface IFileSystem
{
    IFileSystemFile File { get; }
    IFileSystemDirectory Directory { get; }
    IFileSystemPath Path { get; }
}