using PolyType;
using PolyType.Abstractions;
using Shouldly;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.Invocation;
using Tenekon.CommandLine.Extensions.PolyType.Tests.Infrastructure;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Runtime.Binding;

public class ParameterResolutionTests
{
    [Fact]
    public void ResolveOrDefault_FunctionRegistryPrecedesService()
    {
        var bindingContext = new BindingContext(new BindingRegistry(), new CommandRuntimeSettings())
        {
            AllowFunctionResolutionFromServices = true,
            DefaultFunctionResolver = new CommandFunctionRegistry()
        };
        var registry = (CommandFunctionRegistry)bindingContext.DefaultFunctionResolver;
        registry.Set<SampleCallback>(() => "registry");

        var resolver = new DictionaryServiceResolver().Add<SampleCallback>(() => "service");

        var parameter = GetParameterShape<SampleCommand>(nameof(SampleCommand.Run), parameterIndex: 1);
        var resolved = Resolve(parameter, bindingContext, resolver, functionResolver: null) as SampleCallback;

        resolved.ShouldNotBeNull();
        resolved!.Invoke().ShouldBe("registry");
    }

    [Fact]
    public void ResolveOrDefault_OverrideFunctionResolver_OverridesDefault()
    {
        var defaultRegistry = new CommandFunctionRegistry();
        defaultRegistry.Set<SampleCallback>(() => "default");
        var bindingContext = new BindingContext(new BindingRegistry(), new CommandRuntimeSettings())
        {
            AllowFunctionResolutionFromServices = false,
            DefaultFunctionResolver = defaultRegistry
        };
        var overrideResolver = new FixedFunctionResolver<SampleCallback>(new SampleCallback(() => "current"));
        var functionResolver = bindingContext.CreateFunctionResolver(serviceResolver: null, overrideResolver);

        var parameter = GetParameterShape<SampleCommand>(nameof(SampleCommand.Run), parameterIndex: 1);
        var resolved = Resolve(parameter, bindingContext, resolver: null, functionResolver) as SampleCallback;

        resolved.ShouldNotBeNull();
        resolved!.Invoke().ShouldBe("current");
    }

    [Fact]
    public void ResolveOrDefault_FunctionResolutionFromServices_Disabled_Throws()
    {
        var bindingContext = new BindingContext(new BindingRegistry(), new CommandRuntimeSettings())
        {
            AllowFunctionResolutionFromServices = false
        };
        var resolver = new DictionaryServiceResolver().Add<SampleCallback>(() => "service");

        var parameter = GetParameterShape<SampleCommand>(nameof(SampleCommand.Run), parameterIndex: 1);

        Should.Throw<InvalidOperationException>(() => Resolve(
            parameter,
            bindingContext,
            resolver,
            functionResolver: null));
    }

    [Fact]
    public void ResolveOrDefault_FunctionResolutionFromServices_UsesService()
    {
        var bindingContext = new BindingContext(new BindingRegistry(), new CommandRuntimeSettings())
        {
            AllowFunctionResolutionFromServices = true
        };
        var resolver = new DictionaryServiceResolver().Add<SampleCallback>(() => "service");

        var parameter = GetParameterShape<SampleCommand>(nameof(SampleCommand.Run), parameterIndex: 1);
        var resolved = Resolve(parameter, bindingContext, resolver, functionResolver: null) as SampleCallback;

        resolved.ShouldNotBeNull();
        resolved!.Invoke().ShouldBe("service");
    }

    [Theory]
    [CombinatorialData]
    public void ResolveOrDefault_ServiceParameter_ResolvesFromService(
        [CombinatorialValues("alpha", "beta")] string value)
    {
        var bindingContext = new BindingContext(new BindingRegistry(), new CommandRuntimeSettings());
        var resolver = new DictionaryServiceResolver().Add(new SampleDependency(value));

        var parameter = GetParameterShape<SampleCommand>(nameof(SampleCommand.Run), parameterIndex: 0);
        var resolved = Resolve(parameter, bindingContext, resolver, functionResolver: null) as SampleDependency;

        resolved.ShouldNotBeNull();
        resolved!.Value.ShouldBe(value);
    }

    private static object Resolve(
        IParameterShape parameterShape,
        BindingContext bindingContext,
        ICommandServiceResolver? resolver,
        ICommandFunctionResolver? functionResolver)
    {
        return parameterShape.Accept(
            new ParameterResolutionVisitor(),
            new ParameterResolutionState(bindingContext, resolver, functionResolver))!;
    }

    private static IParameterShape GetParameterShape<TCommand>(string methodName, int parameterIndex)
        where TCommand : IShapeable<TCommand>
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<TCommand>();
        var method = shape.Methods.First(m => m.Name == methodName);
        return method.Parameters[parameterIndex];
    }

    private sealed class ParameterResolutionVisitor : TypeShapeVisitor
    {
        public override object? VisitParameter<TArgumentState, TParameterType>(
            IParameterShape<TArgumentState, TParameterType> parameterShape,
            object? state = null)
        {
            var info = (ParameterResolutionState)state!;
            return ParameterResolution.ResolveOrDefault(
                parameterShape,
                info.BindingContext,
                info.ServiceResolver,
                info.FunctionResolver);
        }
    }

    private sealed record ParameterResolutionState(
        BindingContext BindingContext,
        ICommandServiceResolver? ServiceResolver,
        ICommandFunctionResolver? FunctionResolver);

    private sealed class DictionaryServiceResolver : ICommandServiceResolver
    {
        private readonly Dictionary<Type, object> _services = new();

        public DictionaryServiceResolver Add<TService>(TService instance)
        {
            _services[typeof(TService)] = instance!;
            return this;
        }

        public bool TryResolve<TService>(out TService? value)
        {
            if (_services.TryGetValue(typeof(TService), out var resolved))
            {
                value = (TService)resolved;
                return true;
            }

            value = default;
            return false;
        }
    }
}

[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial class SampleCommand
{
    public void Run(SampleDependency dependency, SampleCallback callback) { }
}

public sealed class SampleDependency(string value)
{
    public string Value { get; } = value;
}

public delegate string SampleCallback();