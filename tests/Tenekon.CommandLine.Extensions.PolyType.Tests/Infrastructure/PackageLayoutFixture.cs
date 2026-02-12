using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using CliWrap.Buffered;
using Shouldly;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Infrastructure;

public sealed class PackageLayoutFixture : IAsyncLifetime
{
    private string _outputRoot;

    public string[] Entries { get; set; }

    public void Dispose()
    {
        if (Directory.Exists(_outputRoot)) Directory.Delete(_outputRoot, recursive: true);
    }

    private static async Task<ProcessResult> RunProcessAsync(string fileName, string[] arguments)
    {
        var bufferedResult = await CliWrap.Cli.Wrap(fileName).WithArguments(arguments).ExecuteAsync();

        return new ProcessResult(
            bufferedResult.ExitCode,
            string.Empty);
        
        // return new ProcessResult(
        //     bufferedResult.ExitCode,
        //     string.Concat(bufferedResult.StandardOutput, bufferedResult.StandardError));
    }

    private sealed record ProcessResult(int ExitCode, string Output);

    public async Task InitializeAsync()
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

        var result = await RunProcessAsync(
            "dotnet",
            ["pack", projectPath, "-c", "Release", $"-p:PackageOutputPath={outputPath}"]);

        result.ExitCode.ShouldBe(0, $"dotnet pack failed with exit code {result.ExitCode}\n{result.Output}");

        var nupkg = Directory.GetFiles(outputPath, "*.nupkg").SingleOrDefault();
        string.IsNullOrWhiteSpace(nupkg).ShouldBeFalse("Expected exactly one .nupkg in the package output directory.");

        await using var zip = await ZipFile.OpenReadAsync(nupkg!);
        Entries = zip.Entries.Select(e => e.FullName).ToArray();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}