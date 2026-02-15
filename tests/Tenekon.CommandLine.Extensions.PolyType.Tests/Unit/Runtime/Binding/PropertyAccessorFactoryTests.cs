using PolyType;
using PolyType.Abstractions;
using Shouldly;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Runtime.Binding;

public partial class PropertyAccessorFactoryTests
{
    [Fact]
    public void CreateGetter_PropertyValue_ReturnsValue()
    {
        var shape = TypeShapeResolver.Resolve<AccessorModel>() as IObjectTypeShape;
        shape.ShouldNotBeNull();

        var property = shape!.Properties.First(p => p.Name == nameof(AccessorModel.Name));
        var getter = PropertyAccessorFactory.CreateGetter(property);

        getter.ShouldNotBeNull();
        var model = new AccessorModel { Name = "value" };
        getter!(model).ShouldBe("value");
    }

    [Fact]
    public void CreateSetter_WritableProperty_SetsValue()
    {
        var shape = TypeShapeResolver.Resolve<AccessorModel>() as IObjectTypeShape;
        shape.ShouldNotBeNull();

        var property = shape!.Properties.First(p => p.Name == nameof(AccessorModel.Name));
        var setter = PropertyAccessorFactory.CreateSetter(property);

        setter.ShouldNotBeNull();
        var model = new AccessorModel();
        setter!(model, "updated");
        model.Name.ShouldBe("updated");
    }

    [Fact]
    public void CreateSetter_ReadOnlyProperty_ReturnsNull()
    {
        var shape = TypeShapeResolver.Resolve<AccessorModel>() as IObjectTypeShape;
        shape.ShouldNotBeNull();

        var property = shape!.Properties.First(p => p.Name == nameof(AccessorModel.ReadOnlyValue));
        var setter = PropertyAccessorFactory.CreateSetter(property);

        setter.ShouldBeNull();
    }

    [GenerateShape]
    public partial class AccessorModel
    {
        public string Name { get; set; } = "";

        public int ReadOnlyValue { get; } = 5;
    }
}