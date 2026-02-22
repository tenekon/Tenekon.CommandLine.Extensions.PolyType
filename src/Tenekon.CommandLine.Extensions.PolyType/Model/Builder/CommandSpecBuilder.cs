using System.Collections.Immutable;
using Tenekon.CommandLine.Extensions.PolyType.Spec;

namespace Tenekon.CommandLine.Extensions.PolyType.Model.Builder;

/// <summary>
/// Provides a mutable builder for <see cref="CommandSpecModel"/> instances.
/// </summary>
public sealed class CommandSpecBuilder
{
    private readonly CommandSpecModel _model;
    private CommandSpecModel? _mutable;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandSpecBuilder"/> class.
    /// </summary>
    public CommandSpecBuilder() : this(new CommandSpecModel())
    {
    }

    internal CommandSpecBuilder(CommandSpecModel model)
    {
        _model = model;
    }

    private CommandSpecModel Current => _mutable ?? _model;

    private CommandSpecModel Mutable
    {
        get
        {
            if (_mutable is null) _mutable = _model.Clone();
            return _mutable;
        }
    }

    /// <summary>
    /// Gets or sets the command name.
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
    /// Gets or sets whether the command is hidden from help output.
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
    /// Gets or sets the single alias for the command.
    /// </summary>
    public string? Alias
    {
        get => Current.Alias;
        set => Mutable.Alias = value;
    }

    /// <summary>
    /// Gets or sets the additional aliases for the command.
    /// </summary>
    public ImmutableArray<string> Aliases
    {
        get => Current.Aliases;
        set => Mutable.Aliases = value;
    }

    /// <summary>
    /// Gets or sets the explicit parent command type.
    /// </summary>
    public Type? Parent
    {
        get => Current.Parent;
        set => Mutable.Parent = value;
    }

    /// <summary>
    /// Gets or sets the explicit child command types.
    /// </summary>
    public ImmutableArray<Type> Children
    {
        get => Current.Children;
        set => Mutable.Children = value;
    }

    /// <summary>
    /// Gets or sets whether unmatched tokens should be reported as errors.
    /// </summary>
    public bool TreatUnmatchedTokensAsErrors
    {
        get => Current.TreatUnmatchedTokensAsErrors;
        set => Mutable.TreatUnmatchedTokensAsErrors = value;
    }

    /// <summary>
    /// Gets or sets the auto-generation flags for the command name.
    /// </summary>
    public NameAutoGenerate NameAutoGenerate
    {
        get => Current.NameAutoGenerate;
        set
        {
            var mutable = Mutable;
            mutable.NameAutoGenerate = value;
            mutable.IsNameAutoGenerateSpecified = true;
        }
    }

    /// <summary>
    /// Gets or sets the casing convention for generated names.
    /// </summary>
    public NameCasingConvention NameCasingConvention
    {
        get => Current.NameCasingConvention;
        set
        {
            var mutable = Mutable;
            mutable.NameCasingConvention = value;
            mutable.IsNameCasingConventionSpecified = true;
        }
    }

    /// <summary>
    /// Gets or sets the prefix convention for generated names.
    /// </summary>
    public NamePrefixConvention NamePrefixConvention
    {
        get => Current.NamePrefixConvention;
        set
        {
            var mutable = Mutable;
            mutable.NamePrefixConvention = value;
            mutable.IsNamePrefixConventionSpecified = true;
        }
    }

    /// <summary>
    /// Gets or sets the auto-generation flags for short form names.
    /// </summary>
    public NameAutoGenerate ShortFormAutoGenerate
    {
        get => Current.ShortFormAutoGenerate;
        set
        {
            var mutable = Mutable;
            mutable.ShortFormAutoGenerate = value;
            mutable.IsShortFormAutoGenerateSpecified = true;
        }
    }

    /// <summary>
    /// Gets or sets the prefix convention for generated short form names.
    /// </summary>
    public NamePrefixConvention ShortFormPrefixConvention
    {
        get => Current.ShortFormPrefixConvention;
        set
        {
            var mutable = Mutable;
            mutable.ShortFormPrefixConvention = value;
            mutable.IsShortFormPrefixConventionSpecified = true;
        }
    }

    /// <summary>
    /// Gets or sets whether name auto-generation was explicitly specified.
    /// </summary>
    public bool IsNameAutoGenerateSpecified
    {
        get => Current.IsNameAutoGenerateSpecified;
        set => Mutable.IsNameAutoGenerateSpecified = value;
    }

    /// <summary>
    /// Gets or sets whether name casing was explicitly specified.
    /// </summary>
    public bool IsNameCasingConventionSpecified
    {
        get => Current.IsNameCasingConventionSpecified;
        set => Mutable.IsNameCasingConventionSpecified = value;
    }

    /// <summary>
    /// Gets or sets whether name prefix was explicitly specified.
    /// </summary>
    public bool IsNamePrefixConventionSpecified
    {
        get => Current.IsNamePrefixConventionSpecified;
        set => Mutable.IsNamePrefixConventionSpecified = value;
    }

    /// <summary>
    /// Gets or sets whether short form auto-generation was explicitly specified.
    /// </summary>
    public bool IsShortFormAutoGenerateSpecified
    {
        get => Current.IsShortFormAutoGenerateSpecified;
        set => Mutable.IsShortFormAutoGenerateSpecified = value;
    }

    /// <summary>
    /// Gets or sets whether the short form prefix was explicitly specified.
    /// </summary>
    public bool IsShortFormPrefixConventionSpecified
    {
        get => Current.IsShortFormPrefixConventionSpecified;
        set => Mutable.IsShortFormPrefixConventionSpecified = value;
    }

    /// <summary>
    /// Builds the resulting <see cref="CommandSpecModel"/>.
    /// </summary>
    /// <returns>The built <see cref="CommandSpecModel"/>.</returns>
    internal CommandSpecModel Build()
    {
        return _mutable ?? _model;
    }
}
