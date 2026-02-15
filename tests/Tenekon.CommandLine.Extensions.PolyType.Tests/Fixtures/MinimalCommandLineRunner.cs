using System.CommandLine.Help;
using Microsoft.Extensions.DependencyInjection;
using PolyType;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.Invocation;
using Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Fixtures;

internal static class MinimalCommandLineRunner
{
    private static readonly Type s_describingCommandType = typeof(IDescribingCommand);

    public static async Task<int> RunAsync<TCommand>(
        string[] args,
        Action<IServiceProvider> serviceProviderSetter,
        IServiceCollection secondStageServiceCollection,
        CommandRuntimeSettings settings,
        ITypeShapeProvider? commandShapeProvider) where TCommand : IShapeable<TCommand>
    {
        await using var firstStageServiceProvider = new ServiceCollection().BuildServiceProvider();
        serviceProviderSetter(firstStageServiceProvider);

        var app = CommandRuntime.Factory.Object.Create<TCommand>(settings, serviceResolver: null);

        var currentArgs = args;
        int lastResultCode;
        do
        {
            var result = app.Parse(currentArgs);
            var hasCalledType = result.TryGetCalledType(out var commandCallableType);
            var isDescribing = hasCalledType && commandCallableType is not null
                && s_describingCommandType.IsAssignableFrom(commandCallableType);

            var isParseResultCircuitBreaking = result.ParseResult.Errors.Count > 0
                || result.ParseResult.Action is HelpAction || commandCallableType is null;

            if (!isParseResultCircuitBreaking && commandShapeProvider is not null)
            {
                var commandShape = commandShapeProvider.GetTypeShape(commandCallableType!);
                if (commandShape?.Accept(ConfigureCommandActionInvoker.Default) is Action<ConfigureCommandContext>
                    configureCommandAction)
                {
                    var configureCommandContext = new ConfigureCommandContext(
                        result,
                        secondStageServiceCollection,
                        commandCallableType!);
                    configureCommandAction(configureCommandContext);
                }
            }

            ServiceProvider? secondStageServiceProvider = null;
            try
            {
                if (!isParseResultCircuitBreaking && !isDescribing)
                {
                    var secondStageServiceCollection2 = new ServiceCollection();
                    foreach (var descriptor in secondStageServiceCollection)
                        ((ICollection<ServiceDescriptor>)secondStageServiceCollection2).Add(descriptor);

                    secondStageServiceProvider = secondStageServiceCollection2.BuildServiceProvider();
                    serviceProviderSetter(secondStageServiceProvider);
                }

                var resolver = secondStageServiceProvider is null
                    ? null
                    : new ServiceProviderResolver(secondStageServiceProvider);
                var config = new CommandInvocationOptions { ServiceResolver = resolver };
                lastResultCode = await result.RunAsync(config, CancellationToken.None);

                if (isParseResultCircuitBreaking || lastResultCode != 0) break;

                if (commandCallableType is not null && result.Bind(commandCallableType) is IChainableCommand
                    {
                        ForwardedArguments.Length: > 0
                    } chainable)
                {
                    currentArgs = chainable.ForwardedArguments!;
                    continue;
                }
            }
            finally
            {
                if (secondStageServiceProvider is not null) await secondStageServiceProvider.DisposeAsync();
            }

            break;
        } while (true);

        return lastResultCode;
    }
}