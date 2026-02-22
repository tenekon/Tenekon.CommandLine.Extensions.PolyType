using System.CommandLine;
using System.CommandLine.Parsing;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.FileSystem;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.Validation;
using Tenekon.CommandLine.Extensions.PolyType.Tests.Fixtures;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Runtime.Validation;

public class ValidationHelperTests
{
    [Theory]
    [CombinatorialData]
    public void Apply_FileSystemRules_ValidatePaths(
        [CombinatorialValues(
            ValidationRules.ExistingFile,
            ValidationRules.NonExistingFile,
            ValidationRules.ExistingDirectory,
            ValidationRules.NonExistingDirectory,
            ValidationRules.ExistingFileOrDirectory,
            ValidationRules.NonExistingFileOrDirectory)]
        ValidationRules rules,
        [CombinatorialValues(PathKind.File, PathKind.Directory, PathKind.Missing)] PathKind pathKind)
    {
        var fs = new MockFileSystemFixture();
        var fileSystem = fs.Adapter;
        var value = CreatePath(fs, pathKind);

        var errors = ParseOptionErrors(rules, fileSystem, value);
        var isValid = IsValid(rules, pathKind);

        if (isValid)
            errors.Count.ShouldBe(expected: 0);
        else
            errors.Count.ShouldBeGreaterThan(expected: 0);
    }

    [Theory]
    [CombinatorialData]
    public void Apply_LegalPathAndFileNameRules_ValidateValues(
        [CombinatorialValues(ValidationRules.LegalPath, ValidationRules.LegalFileName)] ValidationRules rules,
        bool isValid)
    {
        var fileSystem = new MockFileSystemFixture().Adapter;
        var value = isValid ? "valid" : CreateInvalidValue(rules, fileSystem);

        var errors = ParseOptionErrors(rules, fileSystem, value);

        if (isValid)
            errors.Count.ShouldBe(expected: 0);
        else
            errors.Count.ShouldBeGreaterThan(expected: 0);
    }

    [Theory]
    [CombinatorialData]
    public void Apply_LegalUriAndUrlRules_ValidateValues(
        [CombinatorialValues(ValidationRules.LegalUri, ValidationRules.LegalUrl)] ValidationRules rules,
        bool isValid)
    {
        var fileSystem = new MockFileSystemFixture().Adapter;
        var value = CreateUriValue(rules, isValid);

        var errors = ParseOptionErrors(rules, fileSystem, value);

        if (isValid)
            errors.Count.ShouldBe(expected: 0);
        else
            errors.Count.ShouldBeGreaterThan(expected: 0);
    }

    [Fact]
    public void Apply_RegexPatternWithMessage_UsesCustomMessage()
    {
        var errors = ParseArgumentErrors(
            ValidationRules.None,
            new MockFileSystemFixture().Adapter,
            "^a+$",
            "pattern-error",
            "bbb");

        errors.Count.ShouldBeGreaterThan(expected: 0);
        errors[index: 0].Message.ShouldContain("pattern-error");
    }

    private static string CreatePath(MockFileSystemFixture fs, PathKind pathKind)
    {
        return pathKind switch
        {
            PathKind.File => fs.CreateFile(),
            PathKind.Directory => fs.CreateDirectory(),
            _ => fs.GetNonExistingPath("missing")
        };
    }

    private static bool IsValid(ValidationRules rules, PathKind pathKind)
    {
        return rules switch
        {
            ValidationRules.ExistingFile => pathKind == PathKind.File,
            ValidationRules.NonExistingFile => pathKind != PathKind.File,
            ValidationRules.ExistingDirectory => pathKind == PathKind.Directory,
            ValidationRules.NonExistingDirectory => pathKind != PathKind.Directory,
            ValidationRules.ExistingFileOrDirectory => pathKind != PathKind.Missing,
            ValidationRules.NonExistingFileOrDirectory => pathKind == PathKind.Missing,
            _ => true
        };
    }

    private static string CreateInvalidValue(ValidationRules rules, IFileSystem fileSystem)
    {
        return rules switch
        {
            ValidationRules.LegalFileName => $"bad{fileSystem.Path.GetInvalidFileNameChars()[0]}name",
            _ => $"bad{fileSystem.Path.GetInvalidPathChars()[0]}path"
        };
    }

    private static string CreateUriValue(ValidationRules rules, bool isValid)
    {
        if (isValid) return "https://example.com";
        if (rules == ValidationRules.LegalUrl) return "ftp://example.com";
        return "not a uri";
    }

    private static IReadOnlyList<ParseError> ParseOptionErrors(
        ValidationRules rules,
        IFileSystem fileSystem,
        string value)
    {
        return ParseOptionErrors(rules, fileSystem, pattern: null, message: null, value);
    }

    private static IReadOnlyList<ParseError> ParseOptionErrors(
        ValidationRules rules,
        IFileSystem fileSystem,
        string? pattern,
        string? message,
        string value)
    {
        var option = new Option<string>("--value");
        ValidationHelper.Apply(option, rules, pattern, message, fileSystem);

        RootCommand command = [option];
        var result = command.Parse(["--value", value]);
        return result.Errors;
    }

    private static IReadOnlyList<ParseError> ParseArgumentErrors(
        ValidationRules rules,
        IFileSystem fileSystem,
        string value)
    {
        return ParseArgumentErrors(rules, fileSystem, pattern: null, message: null, value);
    }

    private static IReadOnlyList<ParseError> ParseArgumentErrors(
        ValidationRules rules,
        IFileSystem fileSystem,
        string? pattern,
        string? message,
        string value)
    {
        var argument = new Argument<string>("value");
        ValidationHelper.Apply(argument, rules, pattern, message, fileSystem);

        RootCommand command = [argument];
        var result = command.Parse([value]);
        return result.Errors;
    }

    public enum PathKind
    {
        File,
        Directory,
        Missing
    }
}
