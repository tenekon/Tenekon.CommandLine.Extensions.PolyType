using System.Collections.Immutable;
using Tenekon.CommandLine.Extensions.PolyType.Spec;

namespace Tenekon.CommandLine.Extensions.PolyType.Model.Builder;

/// <summary>
/// Provides a mutable builder for <see cref="ArgumentSpecModel"/> instances.
/// </summary>
public sealed class ArgumentSpecBuilder
{
    private readonly ArgumentSpecModel _model;
    private ArgumentSpecModel? _mutable;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArgumentSpecBuilder"/> class.
    /// </summary>
    public ArgumentSpecBuilder() : this(new ArgumentSpecModel())
    {
    }

    internal ArgumentSpecBuilder(ArgumentSpecModel model)
    {
        _model = model;
    }

    private ArgumentSpecModel Current => _mutable ?? _model;

    private ArgumentSpecModel Mutable
    {
        get
        {
            if (_mutable is null) _mutable = _model.Clone();
            return _mutable;
        }
    }

    /// <summary>
    /// Gets or sets the argument name.
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
    /// Gets or sets whether the argument is hidden from help output.
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
    /// Gets or sets the help name for the argument.
    /// </summary>
    public string? HelpName
    {
        get => Current.HelpName;
        set => Mutable.HelpName = value;
    }

    /// <summary>
    /// Gets or sets the allowed arity for this argument.
    /// </summary>
    public ArgumentArity Arity
    {
        get => Current.Arity;
        set
        {
            var mutable = Mutable;
            mutable.Arity = value;
            mutable.IsAritySpecified = true;
        }
    }

    /// <summary>
    /// Gets or sets whether the arity was explicitly specified.
    /// </summary>
    public bool IsAritySpecified
    {
        get => Current.IsAritySpecified;
        set => Mutable.IsAritySpecified = value;
    }

    /// <summary>
    /// Gets or sets the allowed values for this argument.
    /// </summary>
    public ImmutableArray<string> AllowedValues
    {
        get => Current.AllowedValues;
        set => Mutable.AllowedValues = value;
    }

    /// <summary>
    /// Gets or sets the built-in validation rules to apply.
    /// </summary>
    public ValidationRules ValidationRules
    {
        get => Current.ValidationRules;
        set => Mutable.ValidationRules = value;
    }

    /// <summary>
    /// Gets or sets the regex pattern used for custom validation.
    /// </summary>
    public string? ValidationPattern
    {
        get => Current.ValidationPattern;
        set => Mutable.ValidationPattern = value;
    }

    /// <summary>
    /// Gets or sets the custom validation message for pattern failures.
    /// </summary>
    public string? ValidationMessage
    {
        get => Current.ValidationMessage;
        set => Mutable.ValidationMessage = value;
    }

    /// <summary>
    /// Gets or sets whether the argument is required.
    /// </summary>
    public bool Required
    {
        get => Current.Required;
        set
        {
            var mutable = Mutable;
            mutable.Required = value;
            mutable.IsRequiredSpecified = true;
        }
    }

    /// <summary>
    /// Gets or sets whether requiredness was explicitly specified.
    /// </summary>
    public bool IsRequiredSpecified
    {
        get => Current.IsRequiredSpecified;
        set => Mutable.IsRequiredSpecified = value;
    }

    /// <summary>
    /// Builds the resulting <see cref="ArgumentSpecModel"/>.
    /// </summary>
    /// <returns>The built <see cref="ArgumentSpecModel"/>.</returns>
    internal ArgumentSpecModel Build()
    {
        return _mutable ?? _model;
    }
}
