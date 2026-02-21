using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Tests.Fixtures;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Runtime;

public class FileSystemValidationIntegrationTests
{
    [Fact]
    public void Parse_ValidationRules_UseConfiguredFileSystem()
    {
        var fs = new MockFileSystemFixture();
        var existingFile = fs.CreateFile("file.txt");
        var existingDir = fs.CreateDirectory("dir");
        var settings = new CommandRuntimeSettings { FileSystem = fs.Adapter };
        var app = CommandRuntime.Factory.Object.Create<FileSystemValidationCommand>(settings);

        var ok = app.Parse(["--file", existingFile, existingDir]);
        ok.ParseResult.Errors.Count.ShouldBe(expected: 0);

        var missingFile = fs.CreateFile("missing.txt");
        var missingDir = fs.CreateFile("missing-dir");
        var bad = app.Parse(["--file", missingFile, missingDir]);
        bad.ParseResult.Errors.Count.ShouldBeGreaterThan(expected: 0);
    }
}