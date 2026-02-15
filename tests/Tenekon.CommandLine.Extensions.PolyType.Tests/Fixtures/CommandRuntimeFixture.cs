using PolyType;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Fixtures;

internal sealed class CommandRuntimeFixture
{
    public CommandRuntimeFixture(Action<CommandRuntimeSettings>? configure = null)
    {
        Output = new StringWriter();
        Error = new StringWriter();
        Settings = new CommandRuntimeSettings
        {
            Output = Output,
            Error = Error
        };

        configure?.Invoke(Settings);
    }

    public StringWriter Output { get; }
    public StringWriter Error { get; }
    public CommandRuntimeSettings Settings { get; }

    public CommandRuntime CreateApp<TCommand>(IServiceProvider? serviceProvider = null)
        where TCommand : IShapeable<TCommand>
    {
        var resolver = serviceProvider is null ? null : new ServiceProviderResolver(serviceProvider);
        return CommandRuntime.Factory.Object.Create<TCommand>(Settings, resolver);
    }

    public CommandRuntimeResult Parse<TCommand>(string[] args, IServiceProvider? serviceProvider = null)
        where TCommand : IShapeable<TCommand>
    {
        return CreateApp<TCommand>(serviceProvider).Parse(args);
    }

    public int Run<TCommand>(string[] args, IServiceProvider? serviceProvider = null)
        where TCommand : IShapeable<TCommand>
    {
        return CreateApp<TCommand>(serviceProvider).Run(args);
    }
}