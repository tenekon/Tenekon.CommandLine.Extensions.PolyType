namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.FileSystem;

public interface IFileSystemPath
{
    char[] GetInvalidPathChars();
    char[] GetInvalidFileNameChars();
}