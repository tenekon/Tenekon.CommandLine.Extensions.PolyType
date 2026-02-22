using System.Collections.Immutable;

namespace Tenekon.CommandLine.Extensions.PolyType.Model.Builder;

/// <summary>
/// Provides a mutable builder for <see cref="CommandHandlerConventionSpecModel"/> instances.
/// </summary>
public sealed class CommandHandlerConventionSpecBuilder
{
    private readonly CommandHandlerConventionSpecModel _model;
    private CommandHandlerConventionSpecModel? _mutable;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandHandlerConventionSpecBuilder"/> class.
    /// </summary>
    public CommandHandlerConventionSpecBuilder() : this(CommandHandlerConventionSpecModel.CreateDefault())
    {
    }

    internal CommandHandlerConventionSpecBuilder(CommandHandlerConventionSpecModel model)
    {
        _model = model;
    }

    private CommandHandlerConventionSpecModel Current => _mutable ?? _model;

    private CommandHandlerConventionSpecModel Mutable
    {
        get
        {
            if (_mutable is null) _mutable = _model.Clone();
            return _mutable;
        }
    }

    /// <summary>
    /// Gets or sets the handler method name conventions.
    /// </summary>
    public ImmutableArray<string> MethodNames
    {
        get => Current.MethodNames;
        set => Mutable.MethodNames = value;
    }

    /// <summary>
    /// Gets or sets whether async handler names are preferred.
    /// </summary>
    public bool PreferAsync
    {
        get => Current.PreferAsync;
        set => Mutable.PreferAsync = value;
    }

    /// <summary>
    /// Gets or sets whether handler conventions are disabled.
    /// </summary>
    public bool Disabled
    {
        get => Current.Disabled;
        set => Mutable.Disabled = value;
    }

    /// <summary>
    /// Builds the resulting <see cref="CommandHandlerConventionSpecModel"/>.
    /// </summary>
    /// <returns>The built <see cref="CommandHandlerConventionSpecModel"/>.</returns>
    internal CommandHandlerConventionSpecModel Build()
    {
        return _mutable ?? _model;
    }
}
