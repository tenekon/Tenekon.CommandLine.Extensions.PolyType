namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Fixtures;

internal sealed class TempFsFixture : IDisposable
{
    public TempFsFixture()
    {
        Root = Path.Combine(
            Path.GetTempPath(),
            "Tenekon.CommandLine.Extensions.PolyType.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
    }

    public string Root { get; }

    public string CreateFile(string? name = null, string? contents = null)
    {
        name ??= Guid.NewGuid().ToString("N") + ".tmp";
        var path = Path.Combine(Root, name);
        File.WriteAllText(path, contents ?? string.Empty);
        return path;
    }

    public string CreateDirectory(string? name = null)
    {
        name ??= Guid.NewGuid().ToString("N");
        var path = Path.Combine(Root, name);
        Directory.CreateDirectory(path);
        return path;
    }

    public string GetNonExistingPath(string? name = null)
    {
        name ??= Guid.NewGuid().ToString("N");
        return Path.Combine(Root, name);
    }

    public void Dispose()
    {
        if (Directory.Exists(Root)) Directory.Delete(Root, recursive: true);
    }
}