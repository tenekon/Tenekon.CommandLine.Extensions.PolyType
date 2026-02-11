using System.CommandLine;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Tests.Fixtures;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Validation;

public class ValidationHelperTests
{
    [Fact]
    public void Apply_ExistingFileRule_ValidatesPaths()
    {
        using var fs = new TempFsFixture();
        var file = fs.CreateFile();
        var missing = fs.GetNonExistingPath("missing.txt");

        ParseOptionErrors(ValidationRules.ExistingFile, file).Count.ShouldBe(expected: 0);
        ParseOptionErrors(ValidationRules.ExistingFile, missing).Count.ShouldBeGreaterThan(expected: 0);
    }

    [Fact]
    public void Apply_NonExistingFileRule_ValidatesPaths()
    {
        using var fs = new TempFsFixture();
        var file = fs.CreateFile();
        var missing = fs.GetNonExistingPath("missing.txt");

        ParseOptionErrors(ValidationRules.NonExistingFile, file).Count.ShouldBeGreaterThan(expected: 0);
        ParseOptionErrors(ValidationRules.NonExistingFile, missing).Count.ShouldBe(expected: 0);
    }

    [Fact]
    public void Apply_ExistingDirectoryRule_ValidatesPaths()
    {
        using var fs = new TempFsFixture();
        var dir = fs.CreateDirectory();
        var missing = fs.GetNonExistingPath("missing-dir");

        ParseArgumentErrors(ValidationRules.ExistingDirectory, dir).Count.ShouldBe(expected: 0);
        ParseArgumentErrors(ValidationRules.ExistingDirectory, missing).Count.ShouldBeGreaterThan(expected: 0);
    }

    [Fact]
    public void Apply_NonExistingDirectoryRule_ValidatesPaths()
    {
        using var fs = new TempFsFixture();
        var dir = fs.CreateDirectory();
        var missing = fs.GetNonExistingPath("missing-dir");

        ParseArgumentErrors(ValidationRules.NonExistingDirectory, dir).Count.ShouldBeGreaterThan(expected: 0);
        ParseArgumentErrors(ValidationRules.NonExistingDirectory, missing).Count.ShouldBe(expected: 0);
    }

    [Fact]
    public void Apply_ExistingFileOrDirectoryRule_ValidatesPaths()
    {
        using var fs = new TempFsFixture();
        var file = fs.CreateFile();
        var dir = fs.CreateDirectory();
        var missing = fs.GetNonExistingPath("missing");

        ParseOptionErrors(ValidationRules.ExistingFileOrDirectory, file).Count.ShouldBe(expected: 0);
        ParseOptionErrors(ValidationRules.ExistingFileOrDirectory, dir).Count.ShouldBe(expected: 0);
        ParseOptionErrors(ValidationRules.ExistingFileOrDirectory, missing).Count.ShouldBeGreaterThan(expected: 0);
    }

    [Fact]
    public void Apply_NonExistingFileOrDirectoryRule_ValidatesPaths()
    {
        using var fs = new TempFsFixture();
        var file = fs.CreateFile();
        var dir = fs.CreateDirectory();
        var missing = fs.GetNonExistingPath("missing");

        ParseOptionErrors(ValidationRules.NonExistingFileOrDirectory, file).Count.ShouldBeGreaterThan(expected: 0);
        ParseOptionErrors(ValidationRules.NonExistingFileOrDirectory, dir).Count.ShouldBeGreaterThan(expected: 0);
        ParseOptionErrors(ValidationRules.NonExistingFileOrDirectory, missing).Count.ShouldBe(expected: 0);
    }

    [Fact]
    public void Apply_LegalPathRule_ValidatesPaths()
    {
        var invalidChar = Path.GetInvalidPathChars()[0];
        var invalid = $"bad{invalidChar}path";

        ParseOptionErrors(ValidationRules.LegalPath, "valid").Count.ShouldBe(expected: 0);
        ParseOptionErrors(ValidationRules.LegalPath, invalid).Count.ShouldBeGreaterThan(expected: 0);
    }

    [Fact]
    public void Apply_LegalFileNameRule_ValidatesNames()
    {
        var invalidChar = Path.GetInvalidFileNameChars()[0];
        var invalid = $"bad{invalidChar}file";

        ParseArgumentErrors(ValidationRules.LegalFileName, "valid.txt").Count.ShouldBe(expected: 0);
        ParseArgumentErrors(ValidationRules.LegalFileName, invalid).Count.ShouldBeGreaterThan(expected: 0);
    }

    [Fact]
    public void Apply_LegalUriRule_ValidatesUris()
    {
        ParseOptionErrors(ValidationRules.LegalUri, "https://example.com").Count.ShouldBe(expected: 0);
        ParseOptionErrors(ValidationRules.LegalUri, "not a uri").Count.ShouldBeGreaterThan(expected: 0);
    }

    [Fact]
    public void Apply_LegalUrlRule_ValidatesUrls()
    {
        ParseOptionErrors(ValidationRules.LegalUrl, "https://example.com").Count.ShouldBe(expected: 0);
        ParseOptionErrors(ValidationRules.LegalUrl, "ftp://example.com").Count.ShouldBeGreaterThan(expected: 0);
    }

    [Fact]
    public void Apply_RegexPatternWithMessage_UsesCustomMessage()
    {
        var errors = ParseArgumentErrors(ValidationRules.None, "^a+$", "pattern-error", "bbb");

        errors.Count.ShouldBeGreaterThan(expected: 0);
        errors[index: 0].Message.ShouldContain("pattern-error");
    }

    private static IReadOnlyList<System.CommandLine.Parsing.ParseError> ParseOptionErrors(
        ValidationRules rules,
        string value)
    {
        return ParseOptionErrors(rules, pattern: null, message: null, value);
    }

    private static IReadOnlyList<System.CommandLine.Parsing.ParseError> ParseOptionErrors(
        ValidationRules rules,
        string? pattern,
        string? message,
        string value)
    {
        var option = new Option<string>("--value");
        ValidationHelper.Apply(option, rules, pattern, message);

        var command = new RootCommand();
        command.Add(option);
        var result = command.Parse(["--value", value]);
        return result.Errors;
    }

    private static IReadOnlyList<System.CommandLine.Parsing.ParseError> ParseArgumentErrors(
        ValidationRules rules,
        string value)
    {
        return ParseArgumentErrors(rules, pattern: null, message: null, value);
    }

    private static IReadOnlyList<System.CommandLine.Parsing.ParseError> ParseArgumentErrors(
        ValidationRules rules,
        string? pattern,
        string? message,
        string value)
    {
        var argument = new Argument<string>("value");
        ValidationHelper.Apply(argument, rules, pattern, message);

        var command = new RootCommand();
        command.Add(argument);
        var result = command.Parse([value]);
        return result.Errors;
    }
}