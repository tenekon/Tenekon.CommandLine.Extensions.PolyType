namespace Tenekon.CommandLine.Extensions.PolyType.Tests.TestModels;

public sealed class DummyDependency
{
}

public partial class BasicRootCommand
{
    public BasicRootCommand(DummyDependency? _ = null) { }
}

public partial class RootWithChildrenCommand
{
    public RootWithChildrenCommand(DummyDependency? _ = null) { }

    public partial class ChildACommand
    {
        public ChildACommand(DummyDependency? _ = null) { }
    }

    public partial class ChildBCommand
    {
        public ChildBCommand(DummyDependency? _ = null) { }
    }
}

public partial class ConflictingParentRoot
{
    public ConflictingParentRoot(DummyDependency? _ = null) { }

    public partial class ConflictingChild
    {
        public ConflictingChild(DummyDependency? _ = null) { }
    }
}

public partial class OtherRoot
{
    public OtherRoot(DummyDependency? _ = null) { }
}

public partial class CycleA
{
    public CycleA(DummyDependency? _ = null) { }
}

public partial class CycleB
{
    public CycleB(DummyDependency? _ = null) { }
}

public partial class MissingCommandSpec
{
    public MissingCommandSpec(DummyDependency? _ = null) { }
}

public partial class OptionSpecCommand
{
    public OptionSpecCommand(DummyDependency? _ = null) { }
}

public partial class OptionDefaultCommand
{
    public OptionDefaultCommand(DummyDependency? _ = null) { }
}

public partial class ArgumentSpecCommand
{
    public ArgumentSpecCommand(DummyDependency? _ = null) { }
}

public partial class ArgumentEnumerableCommand
{
    public ArgumentEnumerableCommand(DummyDependency? _ = null) { }
}

public partial class ValidationCommand
{
    public ValidationCommand(DummyDependency? _ = null) { }
}

public partial class RequiredOptionCommand
{
    public RequiredOptionCommand(DummyDependency? _ = null) { }
}

public partial class OptionalOptionCommand
{
    public OptionalOptionCommand(DummyDependency? _ = null) { }
}

public partial class ValueTypeOptionCommand
{
    public ValueTypeOptionCommand(DummyDependency? _ = null) { }
}

public partial class NullableOptionCommand
{
    public NullableOptionCommand(DummyDependency? _ = null) { }
}

public partial class RequiredArgumentCommand
{
    public RequiredArgumentCommand(DummyDependency? _ = null) { }
}

public partial class EnumerableArgumentCommand
{
    public EnumerableArgumentCommand(DummyDependency? _ = null) { }
}

public partial class OptionalArgumentCommand
{
    public OptionalArgumentCommand(DummyDependency? _ = null) { }
}

public partial class DirectiveCommand
{
    public DirectiveCommand(DummyDependency? _ = null) { }
}

public partial class BundlingCommand
{
    public BundlingCommand(DummyDependency? _ = null) { }
}

public partial class ResponseFileCommand
{
    public ResponseFileCommand(DummyDependency? _ = null) { }
}

public partial class RunCommand
{
    public RunCommand(DummyDependency? _ = null) { }
}

public partial class RunReturnsIntCommand
{
    public RunReturnsIntCommand(DummyDependency? _ = null) { }
}

public partial class RunWithContextCommand
{
    public RunWithContextCommand(DummyDependency? _ = null) { }
}

public partial class RunWithServiceCommand
{
    public RunWithServiceCommand(DummyDependency? _ = null) { }
}

public partial class RunWithContextAndServiceCommand
{
    public RunWithContextAndServiceCommand(DummyDependency? _ = null) { }
}

public partial class RunAsyncCommand
{
    public RunAsyncCommand(DummyDependency? _ = null) { }
}

public partial class RunAsyncWithServiceAndTokenCommand
{
    public RunAsyncWithServiceAndTokenCommand(DummyDependency? _ = null) { }
}

public partial class RunAsyncReturnsIntCommand
{
    public RunAsyncReturnsIntCommand(DummyDependency? _ = null) { }
}

public partial class RunAsyncWithContextCommand
{
    public RunAsyncWithContextCommand(DummyDependency? _ = null) { }
}

public partial class RunAndRunAsyncCommand
{
    public RunAndRunAsyncCommand(DummyDependency? _ = null) { }
}

public partial class NoRunCommand
{
    public NoRunCommand(DummyDependency? _ = null) { }
}

public partial class RunWithContextNotFirstCommand
{
    public RunWithContextNotFirstCommand(DummyDependency? _ = null) { }
}

public partial class RunAsyncWithCancellationTokenCommand
{
    public RunAsyncWithCancellationTokenCommand(DummyDependency? _ = null) { }
}

public partial class RunAsyncWithTokenNotLastCommand
{
    public RunAsyncWithTokenNotLastCommand(DummyDependency? _ = null) { }
}

public partial class RunPrivateCommand
{
    public RunPrivateCommand(DummyDependency? _ = null) { }
}

public partial class RunOverloadConflictCommand
{
    public RunOverloadConflictCommand(DummyDependency? _ = null) { }
}

public partial class RunWithRequiredServiceCommand
{
    public RunWithRequiredServiceCommand(DummyDependency? _ = null) { }
}

public partial class RunWithOptionalServiceCommand
{
    public RunWithOptionalServiceCommand(DummyDependency? _ = null) { }
}

public partial class CollisionRootCommand
{
    public CollisionRootCommand(DummyDependency? _ = null) { }

    public partial class CollisionChildCommand
    {
        public CollisionChildCommand(DummyDependency? _ = null) { }
    }
}

public partial class AliasCollisionRootCommand
{
    public AliasCollisionRootCommand(DummyDependency? _ = null) { }

    public partial class AliasCollisionChildCommand
    {
        public AliasCollisionChildCommand(DummyDependency? _ = null) { }
    }
}

public partial class ExplicitRequiredOptionCommand
{
    public ExplicitRequiredOptionCommand(DummyDependency? _ = null) { }
}

public partial class ExplicitRequiredValueTypeOptionCommand
{
    public ExplicitRequiredValueTypeOptionCommand(DummyDependency? _ = null) { }
}

public partial class InterfaceSpecCommand
{
    public InterfaceSpecCommand(DummyDependency? _ = null) { }
}

public partial class InterfaceSpecConflictCommand
{
    public InterfaceSpecConflictCommand(DummyDependency? _ = null) { }
}

public partial class InterfaceSpecMultipleConflictCommand
{
    public InterfaceSpecMultipleConflictCommand(DummyDependency? _ = null) { }
}

public partial class InterfaceSpecExplicitCommand
{
    public InterfaceSpecExplicitCommand(DummyDependency? _ = null) { }
}

public partial class InterfaceSpecDefaultCommand
{
    public InterfaceSpecDefaultCommand(DummyDependency? _ = null) { }
}

public partial class InterfaceSpecAliasCollisionCommand
{
    public InterfaceSpecAliasCollisionCommand(DummyDependency? _ = null) { }
}

public partial class InterfaceSpecBaseCommand
{
    public InterfaceSpecBaseCommand(DummyDependency? _ = null) { }
}

public partial class InterfaceSpecDerivedCommand
{
    public InterfaceSpecDerivedCommand(DummyDependency? _ = null) { }
}
