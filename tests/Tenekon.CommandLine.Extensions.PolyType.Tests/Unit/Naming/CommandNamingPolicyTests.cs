using Shouldly;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Naming;

public class CommandNamingPolicyTests
{
    [Fact]
    public void GetCommandName_CommandSuffixStripped_ReturnsEmpty()
    {
        var namer = new CommandNamingPolicy(
            NameAutoGenerate.All,
            NameCasingConvention.KebabCase,
            NamePrefixConvention.DoubleHyphen,
            NameAutoGenerate.All,
            NamePrefixConvention.SingleHyphen);

        var name = namer.GetCommandName("RootCliCommand");

        name.ShouldBe(string.Empty);
    }

    [Fact]
    public void GetOptionName_OptionSuffixRemoved_AddsPrefix()
    {
        var namer = new CommandNamingPolicy(
            NameAutoGenerate.All,
            NameCasingConvention.KebabCase,
            NamePrefixConvention.DoubleHyphen,
            NameAutoGenerate.All,
            NamePrefixConvention.SingleHyphen);

        var name = namer.GetOptionName("SearchPathOption");

        name.ShouldBe("--search-path");
    }

    [Fact]
    public void GetArgumentName_ArgumentSuffixRemoved_NoPrefix()
    {
        var namer = new CommandNamingPolicy(
            NameAutoGenerate.All,
            NameCasingConvention.SnakeCase,
            NamePrefixConvention.DoubleHyphen,
            NameAutoGenerate.All,
            NamePrefixConvention.SingleHyphen);

        var name = namer.GetArgumentName("InputCliArgument");

        name.ShouldBe("input_cli");
    }

    [Fact]
    public void GetDirectiveName_DirectiveSuffixRemoved_UsesCasing()
    {
        var namer = new CommandNamingPolicy(
            NameAutoGenerate.All,
            NameCasingConvention.KebabCase,
            NamePrefixConvention.DoubleHyphen,
            NameAutoGenerate.All,
            NamePrefixConvention.SingleHyphen);

        var name = namer.GetDirectiveName("NoRestoreDirective");

        name.ShouldBe("no-restore");
    }

    [Fact]
    public void NormalizeOptionAlias_UnprefixedAlias_AddsConfiguredPrefix()
    {
        var namer = new CommandNamingPolicy(
            NameAutoGenerate.All,
            NameCasingConvention.KebabCase,
            NamePrefixConvention.ForwardSlash,
            NameAutoGenerate.All,
            NamePrefixConvention.SingleHyphen);

        var alias = namer.NormalizeOptionAlias("config", shortForm: false);

        alias.ShouldBe("/config");
    }

    [Fact]
    public void CreateShortForm_AutoGenerateFlags_Honored()
    {
        var namer = new CommandNamingPolicy(
            NameAutoGenerate.None,
            NameCasingConvention.KebabCase,
            NamePrefixConvention.DoubleHyphen,
            NameAutoGenerate.Option,
            NamePrefixConvention.SingleHyphen);

        var optionShortForm = namer.CreateShortForm("verbose", forOption: true);
        var commandShortForm = namer.CreateShortForm("build", forOption: false);

        optionShortForm.ShouldBe("-v");
        commandShortForm.ShouldBeNull();
    }

    [Fact]
    public void CreateShortForm_CommandAliasWithPrefixExists_ReturnsUnprefixedShortForm()
    {
        var namer = new CommandNamingPolicy(
            NameAutoGenerate.All,
            NameCasingConvention.KebabCase,
            NamePrefixConvention.DoubleHyphen,
            NameAutoGenerate.All,
            NamePrefixConvention.SingleHyphen);

        namer.AddAlias("-b");
        var shortForm = namer.CreateShortForm("build", forOption: false);

        shortForm.ShouldBe("b");
    }

    [Fact]
    public void AddAlias_DuplicateAlias_Throws()
    {
        var namer = new CommandNamingPolicy(
            NameAutoGenerate.All,
            NameCasingConvention.KebabCase,
            NamePrefixConvention.DoubleHyphen,
            NameAutoGenerate.All,
            NamePrefixConvention.SingleHyphen);

        namer.AddAlias("--dup");
        Should.Throw<InvalidOperationException>(() => namer.AddAlias("--dup"));
    }
}