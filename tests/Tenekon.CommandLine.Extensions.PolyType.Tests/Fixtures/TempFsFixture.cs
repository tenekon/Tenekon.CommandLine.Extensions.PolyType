namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Fixtures;

internal sealed class TempFsFixture : IDisposable
{
    private readonly string _root;

    public TempFsFixture()
    {
        _root = Path.Combine(
            Path.GetTempPath(),
            "Tenekon.CommandLine.Extensions.PolyType.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public string Root => _root;

    public string CreateFile(string? name = null, string? contents = null)
    {
        name ??= Guid.NewGuid().ToString("N") + ".tmp";
        var path = Path.Combine(_root, name);
        File.WriteAllText(path, contents ?? string.Empty);
        return path;
    }

    public string CreateDirectory(string? name = null)
    {
        name ??= Guid.NewGuid().ToString("N");
        var path = Path.Combine(_root, name);
        Directory.CreateDirectory(path);
        return path;
    }

    public string GetNonExistingPath(string? name = null)
    {
        name ??= Guid.NewGuid().ToString("N");
        return Path.Combine(_root, name);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}