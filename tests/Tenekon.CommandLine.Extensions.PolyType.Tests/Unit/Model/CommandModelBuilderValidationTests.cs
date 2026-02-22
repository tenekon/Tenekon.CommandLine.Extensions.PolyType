using System.Collections.Generic;
using System.Collections.Immutable;
using PolyType;
using PolyType.Abstractions;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Model.Builder;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;
using BuilderMemberCommand = Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels.Builder.BuilderMemberCommand;
using BuilderMethodCommand = Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels.Builder.BuilderMethodCommand;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Model;

public class CommandModelBuilderValidationTests
{
    [Fact]
    public void Validate_MissingRoot_ReturnsDiagnostic()
    {
        var builder = CommandModelBuilder.CreateEmpty();

        var result = builder.Validate();

        result.IsValid.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "TCLM0000");
    }

    [Fact]
    public void Build_MissingRoot_Throws()
    {
        var builder = CommandModelBuilder.CreateEmpty();

        var exception = Should.Throw<CommandModelValidationException>(() => builder.Build());

        exception.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "TCLM0000");
    }

    [Fact]
    public void Validate_Cycle_ReturnsDiagnostic()
    {
        var builder = CommandModelBuilder.CreateEmpty();
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<BuilderMemberCommand>();
        var node = builder.AddObjectCommand(shape.Type, shape.Provider);

        var otherShape = (IObjectTypeShape)TypeShapeResolver.Resolve<RunCommand>();
        var otherNode = builder.AddObjectCommand(otherShape.Type, otherShape.Provider);

        builder.SetParent(node, otherNode);
        builder.SetParent(otherNode, node);
        builder.SetRoot(node);

        var result = builder.Validate();

        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "TCLM0001");
    }

    [Fact]
    public void Validate_GenericFunction_ReturnsDiagnostic()
    {
        var builder = CommandModelBuilder.CreateEmpty();
        var provider = TypeShapeResolver.ResolveDynamicOrThrow<GenericFunctionCommand<int>, FunctionWitness>().Provider;
        var functionShape = provider.GetTypeShape(typeof(GenericFunctionCommand<int>)) as IFunctionTypeShape
            ?? throw new InvalidOperationException("Missing function shape.");
        var specAttribute = functionShape.AttributeProvider.GetCustomAttribute<CommandSpecAttribute>();
        var spec = specAttribute is null ? new CommandSpecModel() : CommandSpecModel.FromAttribute(specAttribute);

        var node = new CommandFunctionModelBuilderNode(functionShape.Type, functionShape, spec);
        GetMutableNodes(builder).Add(node);
        builder.SetRoot(node);

        var result = builder.Validate();

        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "TCLM0100");
    }

    [Fact]
    public void Validate_MethodOverloadsMissingName_ReturnsDiagnostic()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<OverloadNamedOkCommand>();
        var model = CommandModelFactory.BuildFromObject(shape, shape.Provider);
        var builder = model.ToBuilder();
        var root = (CommandObjectModelBuilderNode)builder.Root!;
        var method = root.MethodChildren.First();
        method.Spec.Name = null;

        var result = builder.Validate();

        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "TCLM0120");
    }

    [Fact]
    public void Validate_MethodParentMismatch_ReturnsDiagnostic()
    {
        var builder = CreateBuilder<MethodRootCommand>();
        var root = (CommandObjectModelBuilderNode)builder.Root!;
        var methodNode = root.MethodChildren.Single();
        methodNode.Parent = null;

        var result = builder.Validate();

        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "TCLM0121");
    }

    [Fact]
    public void Validate_InvalidMethodSpecParent_ReturnsDiagnostic()
    {
        var builder = CreateBuilder<MethodRootCommand>();
        var root = (CommandObjectModelBuilderNode)builder.Root!;
        var methodNode = root.MethodChildren.Single();
        methodNode.Spec.Parent = typeof(RunCommand);

        var result = builder.Validate();

        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "TCLM0122");
    }

    [Fact]
    public void Validate_DuplicateMemberSpec_ReturnsDiagnostic()
    {
        var builder = CreateBuilder<BuilderMemberCommand>();
        var root = (CommandObjectModelBuilderNode)builder.Root!;
        var property = root.Shape.Properties.First(p => p.Name == nameof(BuilderMemberCommand.OptionValue));

        var spec = OptionSpecModel.FromAttribute(new OptionSpecAttribute { Name = "opt" });
        root.AddOption(property, spec);
        root.AddOption(property, spec);

        var result = builder.Validate();

        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "TCLM0200");
    }

    [Fact]
    public void Validate_MemberWithoutSpec_ReturnsDiagnostic()
    {
        var builder = CreateBuilder<BuilderMemberCommand>();
        var root = (CommandObjectModelBuilderNode)builder.Root!;
        var property = root.Shape.Properties.First(p => p.Name == nameof(BuilderMemberCommand.OptionValue));
        var member = new CommandMemberSpecBuilder(
            root.DefinitionType,
            property,
            property,
            option: null,
            argument: null,
            directive: null);

        root.Members.Add(member);

        var result = builder.Validate();

        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "TCLM0201");
    }

    [Fact]
    public void Validate_InvalidOwnerType_ReturnsDiagnostic()
    {
        var builder = CreateBuilder<BuilderMemberCommand>();
        var root = (CommandObjectModelBuilderNode)builder.Root!;
        var property = root.Shape.Properties.First(p => p.Name == nameof(BuilderMemberCommand.OptionValue));

        var spec = OptionSpecModel.FromAttribute(new OptionSpecAttribute { Name = "opt" });
        root.AddOption(property, spec, ownerType: typeof(string));

        var result = builder.Validate();

        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "TCLM0202");
    }

    [Fact]
    public void Validate_TargetPropertyNotOnShape_ReturnsDiagnostic()
    {
        var builder = CreateBuilder<BuilderMemberCommand>();
        var root = (CommandObjectModelBuilderNode)builder.Root!;
        var property = root.Shape.Properties.First(p => p.Name == nameof(BuilderMemberCommand.OptionValue));
        var otherShape = (IObjectTypeShape)TypeShapeResolver.Resolve<RunCommand>();
        var targetProperty = otherShape.Properties.First();
        var spec = OptionSpecModel.FromAttribute(new OptionSpecAttribute { Name = "opt" });

        root.Members.Add(new CommandMemberSpecBuilder(
            root.DefinitionType,
            property,
            targetProperty,
            new OptionSpecBuilder(spec),
            argument: null,
            directive: null));

        var result = builder.Validate();

        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "TCLM0203");
    }

    [Fact]
    public void Validate_PropertyNotOnShapeOrInterface_ReturnsDiagnostic()
    {
        var builder = CreateBuilder<BuilderMemberCommand>();
        var root = (CommandObjectModelBuilderNode)builder.Root!;
        var targetProperty = root.Shape.Properties.First(p => p.Name == nameof(BuilderMemberCommand.OptionValue));
        var otherShape = (IObjectTypeShape)TypeShapeResolver.Resolve<RunCommand>();
        var property = otherShape.Properties.First();
        var spec = OptionSpecModel.FromAttribute(new OptionSpecAttribute { Name = "opt" });

        root.Members.Add(new CommandMemberSpecBuilder(
            root.DefinitionType,
            property,
            targetProperty,
            new OptionSpecBuilder(spec),
            argument: null,
            directive: null));

        var result = builder.Validate();

        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "TCLM0204");
    }

    [Theory]
    [InlineData("TCLM0302", typeof(CommandRuntimeContext))]
    [InlineData("TCLM0302", typeof(CancellationToken))]
    public void Validate_SpecOnContextOrToken_ReturnsDiagnostic(string code, Type parameterType)
    {
        var builder = CreateBuilder<MethodInvocationCommand>();
        var root = (CommandObjectModelBuilderNode)builder.Root!;
        var methodNode = root.MethodChildren.Single();
        var parameter = methodNode.MethodShape.Parameters.Single(p => p.ParameterType.Type == parameterType);

        var spec = OptionSpecModel.FromAttribute(new OptionSpecAttribute { Name = "opt" });
        methodNode.SetOption(parameter, spec);

        var result = builder.Validate();

        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == code);
    }

    [Fact]
    public void Validate_ParameterNotInMethod_ReturnsDiagnostic()
    {
        var builder = CreateBuilder<BuilderMethodCommand>();
        var root = (CommandObjectModelBuilderNode)builder.Root!;
        var methodNode = root.MethodChildren.Single();
        var otherShape = (IObjectTypeShape)TypeShapeResolver.Resolve<MethodInvocationCommand>();
        var otherMethod = otherShape.Methods.Single(method => method.Name == nameof(MethodInvocationCommand.Invoke));
        var otherParameter = otherMethod.Parameters.Single(parameter => parameter.Name == "option");
        var spec = OptionSpecModel.FromAttribute(new OptionSpecAttribute { Name = "opt" });

        methodNode.Parameters.Add(new CommandParameterSpecBuilder(
            otherParameter,
            new OptionSpecBuilder(spec),
            argument: null,
            directive: null));

        var result = builder.Validate();

        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "TCLM0300");
    }

    [Fact]
    public void Validate_DuplicateParameterPosition_ReturnsDiagnostic()
    {
        var builder = CreateBuilder<BuilderMethodCommand>();
        var root = (CommandObjectModelBuilderNode)builder.Root!;
        var methodNode = root.MethodChildren.Single();
        var parameter = methodNode.MethodShape.Parameters.Single(p => p.Name == "optionValue");
        var spec = OptionSpecModel.FromAttribute(new OptionSpecAttribute { Name = "opt" });

        methodNode.Parameters.Add(new CommandParameterSpecBuilder(
            parameter,
            new OptionSpecBuilder(spec),
            argument: null,
            directive: null));
        methodNode.Parameters.Add(new CommandParameterSpecBuilder(
            parameter,
            new OptionSpecBuilder(spec),
            argument: null,
            directive: null));

        var result = builder.Validate();

        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "TCLM0301");
    }

    [Fact]
    public void Validate_ParameterWithoutSpec_ReturnsDiagnostic()
    {
        var builder = CreateBuilder<BuilderMethodCommand>();
        var root = (CommandObjectModelBuilderNode)builder.Root!;
        var methodNode = root.MethodChildren.Single();
        var parameter = methodNode.MethodShape.Parameters.Single(p => p.Name == "optionValue");

        methodNode.Parameters.Add(new CommandParameterSpecBuilder(
            parameter,
            option: null,
            argument: null,
            directive: null));

        var result = builder.Validate();

        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "TCLM0303");
    }

    [Fact]
    public void Validate_EmptyHandlerConvention_ReturnsDiagnostic()
    {
        var builder = CreateBuilder<RunCommand>();
        var root = (CommandObjectModelBuilderNode)builder.Root!;
        root.HandlerConvention.MethodNames = ImmutableArray<string>.Empty;

        var result = builder.Validate();

        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "TCLM0110");
    }

    [Fact]
    public void Validate_UnreachableNode_ReturnsDiagnostic()
    {
        var builder = CommandModelBuilder.CreateEmpty();
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<BuilderMemberCommand>();
        var node = builder.AddObjectCommand(shape.Type, shape.Provider);
        builder.SetRoot(node);

        var otherShape = (IObjectTypeShape)TypeShapeResolver.Resolve<RunCommand>();
        builder.AddObjectCommand(otherShape.Type, otherShape.Provider);

        var result = builder.Validate();

        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "TCLM0002");
    }

    private static List<CommandModelBuilderNode> GetMutableNodes(CommandModelBuilder builder)
    {
        return builder.Nodes as List<CommandModelBuilderNode>
            ?? throw new InvalidOperationException("Builder node list is not mutable.");
    }

    private static CommandModelBuilder CreateBuilder<TCommand>() where TCommand : IShapeable<TCommand>
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<TCommand>();
        var model = CommandModelFactory.BuildFromObject(shape, shape.Provider);
        return model.ToBuilder();
    }
}
