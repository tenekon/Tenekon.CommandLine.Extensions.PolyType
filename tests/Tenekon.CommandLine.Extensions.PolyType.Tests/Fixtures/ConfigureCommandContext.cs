using Microsoft.Extensions.DependencyInjection;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Fixtures;

public sealed class ConfigureCommandContext(CommandRuntimeResult result, IServiceCollection services, Type commandType)
{
    public CommandRuntimeResult Result { get; } = result;
    public IServiceCollection Services { get; } = services;
    public Type CommandType { get; } = commandType;

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