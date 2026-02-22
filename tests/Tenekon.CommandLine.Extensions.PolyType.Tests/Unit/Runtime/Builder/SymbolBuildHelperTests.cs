using PolyType;
using PolyType.Abstractions;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.FileSystem;
using Tenekon.CommandLine.Extensions.PolyType.Tests.Infrastructure;
using ArgumentArity = System.CommandLine.ArgumentArity;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Runtime.Builder;

public class SymbolBuildHelperTests
{
    [Fact]
    public void CreateOption_AppliesAliasesAndShortForm()
    {
        var spec = OptionSpecModel.FromAttribute(
            new OptionSpecAttribute
            {
                Alias = "alias",
                Aliases = ["/alt"]
            });
        var namer = TestNamingPolicy.CreateDefault();

        var option = SymbolBuildHelper.CreateOption<string>(
            "--value",
            spec,
            namer,
            required: true,
            new PhysicalFileSystem());

        option.Aliases.ShouldContain("--alias");
        option.Aliases.ShouldContain("/alt");
        option.Aliases.ShouldContain("-v");
        option.Required.ShouldBeTrue();
    }

    [Fact]
    public void CreateArgument_RequiredString_SetsExactlyOneArity()
    {
        var spec = ArgumentSpecModel.FromAttribute(new ArgumentSpecAttribute());
        var namer = TestNamingPolicy.CreateDefault();
        var shape = GetShape(typeof(string));

        var argument = SymbolBuildHelper.CreateArgument<string>(
            "value",
            spec,
            namer,
            required: true,
            shape,
            new PhysicalFileSystem());

        argument.Arity.ShouldBe(ArgumentArity.ExactlyOne);
    }

    [Fact]
    public void CreateArgument_RequiredEnumerable_SetsOneOrMoreArity()
    {
        var spec = ArgumentSpecModel.FromAttribute(new ArgumentSpecAttribute());
        var namer = TestNamingPolicy.CreateDefault();
        var shape = GetShape(typeof(string[]));

        var argument = SymbolBuildHelper.CreateArgument<string[]>(
            "values",
            spec,
            namer,
            required: true,
            shape,
            new PhysicalFileSystem());

        argument.Arity.ShouldBe(ArgumentArity.OneOrMore);
    }

    private static ITypeShape GetShape(Type type)
    {
        var provider = ((IObjectTypeShape)TypeShapeResolver.Resolve<SymbolBuildHelperWitness>()).Provider;
        return provider.GetTypeShape(type)!;
    }
}

[GenerateShape]
[GenerateShapeFor(typeof(string))]
[GenerateShapeFor(typeof(string[]))]
public partial class SymbolBuildHelperWitness;
