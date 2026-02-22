using PolyType;
using PolyType.Abstractions;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Model.Builder;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Model;

public class CommandModelBuilderVisitorTests
{
    [Fact]
    public void Visit_AllNodes_VisitsObjectFunctionAndMethod()
    {
        var builder = CreateBuilder<MethodRootCommand>();
        var provider = TypeShapeResolver.ResolveDynamicOrThrow<FunctionRootCommand, FunctionWitness>().Provider;
        builder.AddFunctionCommand(typeof(FunctionRootCommand), provider);

        var visitor = new CountingVisitor();
        builder.Visit(visitor);

        visitor.ObjectCount.ShouldBe(expected: 2);
        visitor.MethodCount.ShouldBe(expected: 1);
        visitor.FunctionCount.ShouldBe(expected: 1);
        visitor.MemberCount.ShouldBeGreaterThan(expected: 0);
        visitor.ParameterCount.ShouldBeGreaterThan(expected: 0);
    }

    [Fact]
    public void Visit_BaseCallControlsChildTiming()
    {
        var builder = CreateBuilder<MethodRootCommand>();

        var visitWithBase = new ParentAwareVisitor(callBase: true);
        builder.Visit(visitWithBase);
        visitWithBase.MethodVisitedDuringParent.ShouldBeTrue();

        var visitWithoutBase = new ParentAwareVisitor(callBase: false);
        builder.Visit(visitWithoutBase);
        visitWithoutBase.MethodVisitedDuringParent.ShouldBeFalse();
    }

    [Fact]
    public void Visit_AllNodes_IncludesUnreachable()
    {
        var builder = CreateBuilder<MethodRootCommand>();
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<RunCommand>();
        builder.AddObjectCommand(shape.Type, shape.Provider);

        var visitor = new CommandTypeCollector();
        builder.Visit(visitor);

        visitor.CommandTypes.ShouldContain(typeof(RunCommand));
    }

    private static CommandModelBuilder CreateBuilder<TCommand>() where TCommand : IShapeable<TCommand>
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<TCommand>();
        var model = CommandModelFactory.BuildFromObject(shape, shape.Provider);
        return model.ToBuilder();
    }

    private sealed class CountingVisitor : CommandModelBuilderNodeVisitor
    {
        public int ObjectCount { get; private set; }
        public int FunctionCount { get; private set; }
        public int MethodCount { get; private set; }
        public int MemberCount { get; private set; }
        public int ParameterCount { get; private set; }

        public override void VisitObject(CommandObjectModelBuilderNode node)
        {
            ObjectCount++;
            base.VisitObject(node);
        }

        public override void VisitFunction(CommandFunctionModelBuilderNode node)
        {
            FunctionCount++;
            base.VisitFunction(node);
        }

        public override void VisitMethod(CommandMethodModelBuilderNode node)
        {
            MethodCount++;
            base.VisitMethod(node);
        }

        public override void VisitMember(NodeVisitContext context, CommandMemberSpecBuilder member)
        {
            MemberCount++;
        }

        public override void VisitParameter(NodeVisitContext context, CommandParameterSpecBuilder parameter)
        {
            ParameterCount++;
        }
    }

    private sealed class ParentAwareVisitor(bool callBase) : CommandModelBuilderNodeVisitor
    {
        private bool _insideParent;

        public bool MethodVisitedDuringParent { get; private set; }

        public override void VisitObject(CommandObjectModelBuilderNode node)
        {
            _insideParent = true;
            if (callBase) base.VisitObject(node);
            _insideParent = false;
        }

        public override void VisitMethod(CommandMethodModelBuilderNode node)
        {
            if (_insideParent) MethodVisitedDuringParent = true;
            base.VisitMethod(node);
        }
    }

    private sealed class CommandTypeCollector : CommandModelBuilderNodeVisitor
    {
        public HashSet<Type> CommandTypes { get; } = [];

        public override void VisitObject(CommandObjectModelBuilderNode node)
        {
            CommandTypes.Add(node.DefinitionType);
            base.VisitObject(node);
        }

        public override void VisitFunction(CommandFunctionModelBuilderNode node)
        {
            CommandTypes.Add(node.FunctionType);
            base.VisitFunction(node);
        }
    }
}
