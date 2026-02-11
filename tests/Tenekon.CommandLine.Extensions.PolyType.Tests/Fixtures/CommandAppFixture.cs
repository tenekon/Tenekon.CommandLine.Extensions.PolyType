using PolyType;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Fixtures;

internal sealed class CommandAppFixture
{
    public CommandAppFixture(Action<CommandLineSettings>? configure = null)
    {
        Output = new StringWriter();
        Error = new StringWriter();
        Settings = new CommandLineSettings
        {
            Output = Output,
            Error = Error
        };

        configure?.Invoke(Settings);
    }

    public StringWriter Output { get; }
    public StringWriter Error { get; }
    public CommandLineSettings Settings { get; }

    public CommandLineApp CreateApp<TCommand>(IServiceProvider? serviceProvider = null)
        where TCommand : IShapeable<TCommand>
    {
        return CommandLineApp.CreateFromType<TCommand>(Settings, serviceProvider);
    }

    public CommandLineResult Parse<TCommand>(string[] args, IServiceProvider? serviceProvider = null)
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