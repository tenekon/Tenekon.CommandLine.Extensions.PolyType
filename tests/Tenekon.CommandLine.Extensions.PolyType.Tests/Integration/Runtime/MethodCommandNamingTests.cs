using PolyType.Abstractions;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Runtime;

public class MethodCommandNamingTests
{
    [Fact]
    public void Build_RuntimeThrowsOnDuplicateMethodCommandName()
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<OverloadNamedCollisionCommand>();
        var model = CommandModelBuilder.BuildFromObject(shape, shape.Provider);

        Should.Throw<InvalidOperationException>(() => CommandRuntimeBuilder.Build(model, new CommandRuntimeSettings()));
    }
}