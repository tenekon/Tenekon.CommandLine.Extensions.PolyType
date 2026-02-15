namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Fixtures;

internal sealed class ServiceProviderResolver : ICommandServiceResolver
{
    private readonly IServiceProvider _provider;

    public ServiceProviderResolver(IServiceProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public bool TryResolve<TService>(out TService? value)
    {
        if (_provider is null)
        {
            value = default;
            return false;
        }

        var resolved = _provider.GetService(typeof(TService));
        if (resolved is null)
        {
            value = default;
            return false;
        }

        value = (TService)resolved;
        return true;
    }
}