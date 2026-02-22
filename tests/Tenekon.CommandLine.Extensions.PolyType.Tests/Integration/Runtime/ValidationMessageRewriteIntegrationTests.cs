using System.ComponentModel.DataAnnotations;
using PolyType.Abstractions;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Model.Builder;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Runtime;

public class ValidationMessageRewriteIntegrationTests
{
    [Theory]
    [MemberData(nameof(RewriteCases))]
    public void Parse_ValidationMessage_RewrittenByVisitor_IsReported(
        Func<CommandRuntime> createRuntime,
        string[] args)
    {
        var runtime = createRuntime();

        var result = runtime.Parse(args);

        result.ParseResult.Errors.Any(error => error.Message == "option-display-message")
            .ShouldBeTrue();

        result.ParseResult.Errors.Any(error => error.Message == "argument-display-message")
            .ShouldBeTrue();
    }

    public static IEnumerable<object[]> RewriteCases()
    {
        yield return
        [
            () =>
            {
                var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<ValidationCommand>();
                var model = CommandModelFactory.BuildFromObject(shape, shape.Provider);
                var builder = model.ToBuilder();
                builder.Visit(new ValidationMessageRewriteVisitor());

                return CommandRuntime.Factory.CreateFromModel(
                    builder.Build(),
                    new CommandRuntimeSettings { ShowHelpOnEmptyCommand = false },
                    serviceResolver: null);
            },
            new[] { "--option", "A", "1" }
        ];

        yield return
        [
            () =>
            {
                var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<MethodInvocationCommand>();
                var model = CommandModelFactory.BuildFromObject(shape, shape.Provider);
                var builder = model.ToBuilder();
                builder.Visit(new ValidationMessageRewriteVisitor());

                return CommandRuntime.Factory.CreateFromModel(
                    builder.Build(),
                    new CommandRuntimeSettings(),
                    serviceResolver: null);
            },
            new[] { "invoke", "--opt", "ABC", "bad" }
        ];

        yield return
        [
            () =>
            {
                var shape = (IFunctionTypeShape)TypeShapeResolver.ResolveDynamicOrThrow<FunctionRootCommand, FunctionWitness>();
                var model = CommandModelFactory.BuildFromFunction(shape, shape.Provider);
                var builder = model.ToBuilder();
                builder.Visit(new ValidationMessageRewriteVisitor());

                return CommandRuntime.Factory.CreateFromModel(
                    builder.Build(),
                    new CommandRuntimeSettings(),
                    serviceResolver: null);
            },
            new[] { "--opt", "ABC", "bad" }
        ];
    }

    private sealed class ValidationMessageRewriteVisitor : CommandModelBuilderNodeVisitor
    {
        public override void VisitOption(SpecVisitContext context, OptionSpecBuilder option)
        {
            var display = GetDisplayAttribute(context);
            if (!string.IsNullOrWhiteSpace(display?.GetDescription()))
                option.ValidationMessage = display.GetDescription();

            option.ValidationPattern = "^[a-z]+$";
        }

        public override void VisitArgument(SpecVisitContext context, ArgumentSpecBuilder argument)
        {
            var display = GetDisplayAttribute(context);
            if (!string.IsNullOrWhiteSpace(display?.GetDescription()))
                argument.ValidationMessage = display.GetDescription();

            argument.ValidationPattern = "^[A-Z]+$";
        }

        private static DisplayAttribute? GetDisplayAttribute(SpecVisitContext context)
        {
            if (context.Member is not null)
                return context.Member.SpecProperty.AttributeProvider.GetCustomAttribute<DisplayAttribute>();
            if (context.Parameter is not null)
                return context.Parameter.Parameter.AttributeProvider.GetCustomAttribute<DisplayAttribute>();
            return null;
        }
    }
}
