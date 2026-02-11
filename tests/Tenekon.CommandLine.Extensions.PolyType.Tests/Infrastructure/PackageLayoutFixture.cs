using System.Diagnostics;
using System.IO.Compression;
using Shouldly;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Infrastructure;

public sealed class PackageLayoutFixture : IDisposable
{
    private readonly string _outputRoot;

    public PackageLayoutFixture()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var projectPath = Path.Combine(
            repoRoot,
            "src",
            "Tenekon.CommandLine.Extensions.PolyType",
            "Tenekon.CommandLine.Extensions.PolyType.csproj");

        _outputRoot = Path.Combine(
            Path.GetTempPath(),
            "Tenekon.CommandLine.Extensions.PolyType.PackTests",
            Guid.NewGuid().ToString("N"));
        var outputPath = Path.Combine(_outputRoot, "pkgs");

        Directory.CreateDirectory(outputPath);

        var result = RunProcess("dotnet", $"pack \"{projectPath}\" -c Release -p:PackageOutputPath=\"{outputPath}\"");

        result.ExitCode.ShouldBe(0, $"dotnet pack failed with exit code {result.ExitCode}\n{result.Output}");

        var nupkg = Directory.GetFiles(outputPath, "*.nupkg").SingleOrDefault();
        string.IsNullOrWhiteSpace(nupkg).ShouldBeFalse("Expected exactly one .nupkg in the package output directory.");

        using var zip = ZipFile.OpenRead(nupkg!);
        Entries = zip.Entries.Select(e => e.FullName).ToArray();
    }

    public string[] Entries { get; }

    public void Dispose()
    {
        if (Directory.Exists(_outputRoot)) Directory.Delete(_outputRoot, recursive: true);
    }

    private static ProcessResult RunProcess(string fileName, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new ProcessResult(process.ExitCode, string.Concat(output, error));
    }

    private sealed record ProcessResult(int ExitCode, string Output);
}