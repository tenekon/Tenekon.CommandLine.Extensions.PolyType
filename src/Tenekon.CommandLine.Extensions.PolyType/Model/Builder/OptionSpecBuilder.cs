using System.Collections.Immutable;
using Tenekon.CommandLine.Extensions.PolyType.Spec;

namespace Tenekon.CommandLine.Extensions.PolyType.Model.Builder;

/// <summary>
/// Provides a mutable builder for <see cref="OptionSpecModel"/> instances.
/// </summary>
public sealed class OptionSpecBuilder
{
    private readonly OptionSpecModel _model;
    private OptionSpecModel? _mutable;

    /// <summary>
    /// Initializes a new instance of the <see cref="OptionSpecBuilder"/> class.
    /// </summary>
    public OptionSpecBuilder() : this(new OptionSpecModel())
    {
    }

    internal OptionSpecBuilder(OptionSpecModel model)
    {
        _model = model;
    }

    private OptionSpecModel Current => _mutable ?? _model;

    private OptionSpecModel Mutable
    {
        get
        {
            if (_mutable is null) _mutable = _model.Clone();
            return _mutable;
        }
    }

    /// <summary>
    /// Gets or sets the option name.
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
    /// Gets or sets whether the option is hidden from help output.
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
    /// Gets or sets the single alias for the option.
    /// </summary>
    public string? Alias
    {
        get => Current.Alias;
        set => Mutable.Alias = value;
    }

    /// <summary>
    /// Gets or sets the additional aliases for the option.
    /// </summary>
    public ImmutableArray<string> Aliases
    {
        get => Current.Aliases;
        set => Mutable.Aliases = value;
    }

    /// <summary>
    /// Gets or sets the help name for the option argument.
    /// </summary>
    public string? HelpName
    {
        get => Current.HelpName;
        set => Mutable.HelpName = value;
    }

    /// <summary>
    /// Gets or sets whether the option is recursive.
    /// </summary>
    public bool Recursive
    {
        get => Current.Recursive;
        set => Mutable.Recursive = value;
    }

    /// <summary>
    /// Gets or sets the allowed arity for this option.
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
    /// Gets or sets the allowed values for this option.
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
    /// Gets or sets whether multiple arguments can be provided in a single token.
    /// </summary>
    public bool AllowMultipleArgumentsPerToken
    {
        get => Current.AllowMultipleArgumentsPerToken;
        set => Mutable.AllowMultipleArgumentsPerToken = value;
    }

    /// <summary>
    /// Gets or sets whether the option is required.
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
    /// Builds the resulting <see cref="OptionSpecModel"/>.
    /// </summary>
    /// <returns>The built <see cref="OptionSpecModel"/>.</returns>
    internal OptionSpecModel Build()
    {
        return _mutable ?? _model;
    }
}
