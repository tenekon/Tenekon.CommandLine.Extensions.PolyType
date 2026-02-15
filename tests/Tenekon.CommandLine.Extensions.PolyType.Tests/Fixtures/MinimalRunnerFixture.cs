using Microsoft.Extensions.DependencyInjection;
using PolyType;
using PolyType.Abstractions;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Fixtures;

internal sealed class MinimalRunnerFixture
{
    public MinimalRunnerFixture()
    {
        Output = new StringWriter();
        Error = new StringWriter();
        Settings = new CommandRuntimeSettings
        {
            Output = Output,
            Error = Error,
            ShowHelpOnEmptyCommand = false
        };
    }

    public StringWriter Output { get; }
    public StringWriter Error { get; }
    public CommandRuntimeSettings Settings { get; }
    public IServiceCollection SecondStageServices { get; } = new ServiceCollection();
    public IServiceProvider? FirstStageProvider { get; private set; }
    public List<IServiceProvider> SecondStageProviders { get; } = [];

    public Task<int> RunAsync<TCommand>(string[] args) where TCommand : IShapeable<TCommand>
    {
        return MinimalCommandLineRunner.RunAsync<TCommand>(
            args,
            RecordProvider,
            SecondStageServices,
            Settings,
            GetShapeProvider<TCommand>());
    }

    private void RecordProvider(IServiceProvider provider)
    {
        if (FirstStageProvider is null)
        {
            FirstStageProvider = provider;
            return;
        }

        SecondStageProviders.Add(provider);
    }

    private static ITypeShapeProvider GetShapeProvider<TCommand>() where TCommand : IShapeable<TCommand>
    {
        var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<TCommand>();
        return shape.Provider;
    }
}