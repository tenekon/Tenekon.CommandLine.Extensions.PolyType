using Testably.Abstractions.Testing;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Fixtures;

internal sealed class MockFileSystemFixture
{
    public MockFileSystemFixture()
    {
        Root = FileSystem.Path.Combine("C:\\", "mock-root");
        FileSystem.Directory.CreateDirectory(Root);
        Adapter = new MockFileSystemAdapter(FileSystem);
    }

    public MockFileSystem FileSystem { get; } = new();

    public MockFileSystemAdapter Adapter { get; }
    public string Root { get; }

    public string CreateFile(string? name = null, string? contents = null)
    {
        name ??= Guid.NewGuid().ToString("N") + ".tmp";
        var path = FileSystem.Path.Combine(Root, name);
        FileSystem.File.WriteAllText(path, contents ?? string.Empty);
        return path;
    }

    public string CreateDirectory(string? name = null)
    {
        name ??= Guid.NewGuid().ToString("N");
        var path = FileSystem.Path.Combine(Root, name);
        FileSystem.Directory.CreateDirectory(path);
        return path;
    }

    public string GetNonExistingPath(string? name = null)
    {
        name ??= Guid.NewGuid().ToString("N");
        return FileSystem.Path.Combine(Root, name);
    }
}