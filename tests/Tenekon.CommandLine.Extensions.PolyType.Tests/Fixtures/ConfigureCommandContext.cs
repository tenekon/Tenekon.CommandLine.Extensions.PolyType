using Microsoft.Extensions.DependencyInjection;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Fixtures;

public sealed class ConfigureCommandContext
{
    public ConfigureCommandContext(CommandLineResult result, IServiceCollection services, Type commandType)
    {
        Result = result;
        Services = services;
        CommandType = commandType;
    }

    public CommandLineResult Result { get; }
    public IServiceCollection Services { get; }
    public Type CommandType { get; }

    public void BindCommandProperties<TCommand>(TCommand instance)
    {
        BindCommandProperties(typeof(TCommand), instance!);
    }

    public void BindCommandProperties(Type commandType, object instance)
    {
        if (!Result.TryGetBinder(commandType, instance.GetType(), out var binder) || binder is null)
            throw new InvalidOperationException(
                $"Binder is not found for command '{commandType.FullName}' and target '{instance.GetType().FullName}'.");

        binder(instance, Result.ParseResult);
    }
}
