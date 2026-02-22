namespace Tenekon.CommandLine.Extensions.PolyType.Model.Builder;

/// <summary>
/// Provides a mutable builder for <see cref="DirectiveSpecModel"/> instances.
/// </summary>
public sealed class DirectiveSpecBuilder
{
    private readonly DirectiveSpecModel _model;
    private DirectiveSpecModel? _mutable;

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectiveSpecBuilder"/> class.
    /// </summary>
    public DirectiveSpecBuilder() : this(new DirectiveSpecModel())
    {
    }

    internal DirectiveSpecBuilder(DirectiveSpecModel model)
    {
        _model = model;
    }

    private DirectiveSpecModel Current => _mutable ?? _model;

    private DirectiveSpecModel Mutable
    {
        get
        {
            if (_mutable is null) _mutable = _model.Clone();
            return _mutable;
        }
    }

    /// <summary>
    /// Gets or sets the directive name.
    /// </summary>
    public string? Name
    {
        get => Current.Name;
        set => Mutable.Name = value;
    }

    /// <summary>
    /// Gets or sets the help text shown in usage output.
    /// </summary>
    public string? Description
    {
        get => Current.Description;
        set => Mutable.Description = value;
    }

    /// <summary>
    /// Gets or sets whether the directive is hidden from help output.
    /// </summary>
    public bool Hidden
    {
        get => Current.Hidden;
        set => Mutable.Hidden = value;
    }

    /// <summary>
    /// Gets or sets the ordering value used for help layout.
    /// </summary>
    public int Order
    {
        get => Current.Order;
        set => Mutable.Order = value;
    }

    /// <summary>
    /// Builds the resulting <see cref="DirectiveSpecModel"/>.
    /// </summary>
    /// <returns>The built <see cref="DirectiveSpecModel"/>.</returns>
    internal DirectiveSpecModel Build()
    {
        return _mutable ?? _model;
    }
}
