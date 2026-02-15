# Tenekon.CommandLine.Extensions.PolyType <!-- omit from toc -->

[![Build](https://github.com/tenekon/Tenekon.CommandLine.Extensions.PolyType/actions/workflows/coverage.yml/badge.svg?branch=main)](https://github.com/tenekon/Tenekon.CommandLine.Extensions.PolyType/actions/workflows/coverage.yml)
[![NuGet](https://img.shields.io/nuget/v/Tenekon.CommandLine.Extensions.PolyType.svg)](https://www.nuget.org/packages/Tenekon.CommandLine.Extensions.PolyType)
[![Codecov](https://codecov.io/gh/tenekon/Tenekon.CommandLine.Extensions.PolyType/branch/main/graph/badge.svg)](https://codecov.io/gh/tenekon/Tenekon.CommandLine.Extensions.PolyType)
[![License](https://img.shields.io/github/license/tenekon/Tenekon.CommandLine.Extensions.PolyType.svg)](LICENSE)

Tenekon.CommandLine.Extensions.PolyType adds an attribute-driven layer on top of System.CommandLine, powered by [PolyType shape generation](https://eiriktsarpalis.github.io/PolyType/docs/shape-providers.html). You define commands, options, and arguments with attributes, and get fast, strongly-typed binding without runtime reflection. It supports class commands and function commands and is trimming/AOT friendly.

> [!NOTE]
> This project was crafted in a very short time. The public API is intended to be stable, but changes **may** occur as the library matures.

## Install <!-- omit from toc -->

```console
dotnet add package Tenekon.CommandLine.Extensions.PolyType
```

# Table of Content <!-- omit from toc -->

- [Prerequisites](#prerequisites)
- [Quick Start (Class-Based)](#quick-start-class-based)
- [Quick Start (Function-Based)](#quick-start-function-based)
- [Concepts at a Glance](#concepts-at-a-glance)
- [Defining Commands](#defining-commands)
- [Defining Options and Arguments](#defining-options-and-arguments)
- [Defining Directives](#defining-directives)
- [Handler Methods and Signatures](#handler-methods-and-signatures)
- [Method Commands (Instance Methods)](#method-commands-instance-methods)
- [Interface-Based Specs](#interface-based-specs)
- [Naming Rules and Aliases](#naming-rules-and-aliases)
- [Requiredness and Arity](#requiredness-and-arity)
- [Validation and Allowed Values](#validation-and-allowed-values)
- [Runtime Creation](#runtime-creation)
- [Runtime Creation: Class-Based](#runtime-creation-class-based)
- [Runtime Creation: Custom ITypeShapeProvider](#runtime-creation-custom-itypeshapeprovider)
- [Runtime Creation: Model-First (Advanced)](#runtime-creation-model-first-advanced)
- [Runtime Creation: Function-Based](#runtime-creation-function-based)
- [Parsing Without Invocation](#parsing-without-invocation)
- [Binding Results and Helpers](#binding-results-and-helpers)
- [Invocation Options (Per Call)](#invocation-options-per-call)
- [CommandRuntimeContext](#commandruntimecontext)
- [Services and Service Resolution](#services-and-service-resolution)
- [Functions and Function Resolution](#functions-and-function-resolution)
- [Settings (CommandRuntimeSettings)](#settings-commandruntimesettings)
- [Response Files and POSIX Bundling](#response-files-and-posix-bundling)
- [Built-In Directives](#built-in-directives)
- [File System Abstraction (Advanced)](#file-system-abstraction-advanced)
- [Trimming and AOT](#trimming-and-aot)


## Prerequisites 

- Use [PolyType source generation](https://eiriktsarpalis.github.io/PolyType/docs/shape-providers.html) via `[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]` and `[GenerateShapeFor(IncludeMethods = MethodShapeFlags.AllPublic)]`.
- Command types must be `partial`.
- Target any project that can reference `netstandard2.0` (the package also ships `net10.0`).

## Quick Start (Class-Based)

`Program.cs`:
```csharp
using Tenekon.CommandLine.Extensions.PolyType.Runtime;

return CommandRuntime.Factory.Object
    .Create<RootCommand>(
        settings: null,
        modelRegistry: null,
        modelBuildOptions: null,
        serviceResolver: null)
    .Run(args);
```

Command type:
```csharp
using PolyType;
using PolyType.SourceGenModel;
using Tenekon.CommandLine.Extensions.PolyType.Spec;
using Tenekon.CommandLine.Extensions.PolyType.Runtime;

[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
[CommandSpec(Description = "Root command")]
public partial class RootCommand
{
    [OptionSpec(Description = "Greeting target")]
    public string Name { get; set; } = "world";

    [ArgumentSpec(Description = "Input file")]
    public string? File { get; set; }

    public int Run(CommandRuntimeContext context)
    {
        if (context.IsEmptyCommand())
        {
            context.ShowHelp();
            return 0;
        }

        Console.WriteLine($"Hello {Name}");
        Console.WriteLine($"File = {File}");
        return 0;
    }
}
```

## Quick Start (Function-Based)

`Program.cs`:
Preparation:
```csharp
using PolyType;
using Tenekon.CommandLine.Extensions.PolyType.Spec;

[GenerateShapeFor(typeof(GreetCommand))]
public partial class CliShapes;

[CommandSpec(Description = "Greets from a function")]
public delegate int GreetCommand([OptionSpec] string name);
```

> [!TIP]
> GenerateShapeForAttribute supports [glob pattern](https://eiriktsarpalis.github.io/PolyType/docs/shape-providers.html#source-generator)

> [!NOTE]
> Function commands require a function instance. Register it via `runtime.FunctionRegistry` or provide a custom `ICommandFunctionResolver`.

```csharp
using Tenekon.CommandLine.Extensions.PolyType.Runtime;

var runtime = CommandRuntime.Factory.Function.Create<GreetCommand, CliShapes>(
    settings: null,
    modelRegistry: null,
    modelBuildOptions: null,
    serviceResolver: null);

runtime.FunctionRegistry.Set<GreetCommand>(name =>
{
    Console.WriteLine($"Hello {name}");
    return 0;
});

return runtime.Run(args);
```

Explicit:
```csharp
using PolyType;
using PolyType.Abstractions;
using Tenekon.CommandLine.Extensions.PolyType.Runtime;

var shape = (IFunctionTypeShape)TypeShapeResolver.Resolve<GreetCommand>();
var provider = shape.Provider;

var runtime = CommandRuntime.Factory.Function.Create(
    commandType: typeof(GreetCommand),
    commandTypeShapeProvider: provider,
    settings: null,
    modelRegistry: null,
    modelBuildOptions: null,
    serviceResolver: null);

runtime.FunctionRegistry.Set<GreetCommand>(name =>
{
    Console.WriteLine($"Hello {name}");
    return 0;
});

return runtime.Run(args);
```


## Concepts at a Glance

- Spec attributes describe commands, options, arguments, and directives.
- Model (Advanced) represents the metadata graph derived from shapes.
- Runtime builds System.CommandLine commands and binders from the model.
- Binding maps parsed tokens to strongly-typed objects.
- Invocation runs handlers with services and functions resolved.

## Defining Commands

Use `[CommandSpec]` on classes to define commands.

Nested child commands:
```csharp
[CommandSpec(Name = "git", Description = "Root command")]
public partial class GitCommand
{
    [CommandSpec(Name = "status")]
    public partial class StatusCommand
    {
        public int Run() => 0;
    }
}
```

Explicit parent/child linkage:
```csharp
[CommandSpec(Description = "Root", Children = new[] { typeof(StatusCommand) })]
public partial class RootCommand { }

[CommandSpec(Parent = typeof(RootCommand), Description = "Child")]
public partial class StatusCommand { }
```

You can also control unmatched tokens with `TreatUnmatchedTokensAsErrors` on `[CommandSpec]`.

## Defining Options and Arguments

Property-based definitions:
```csharp
[CommandSpec]
public partial class BuildCommand
{
    [OptionSpec(Description = "Configuration")]
    public string? Configuration { get; set; }

    [ArgumentSpec(Description = "Project path")]
    public string Project { get; set; } = "";

    public int Run() => 0;
}
```

Parameter-based definitions:
```csharp
[CommandSpec]
public partial class CleanCommand
{
    public int Run([OptionSpec] bool force, [ArgumentSpec] string path) => 0;
}
```

`OptionSpecAttribute` and `ArgumentSpecAttribute` support:
- Name, Description, Hidden, Order
- Arity, Required
- AllowedValues
- ValidationRules, ValidationPattern, ValidationMessage

`OptionSpecAttribute` also supports:
- Alias, Aliases, HelpName
- Recursive
- AllowMultipleArgumentsPerToken

## Defining Directives

Define directives with `[DirectiveSpec]` on properties or parameters. Supported types are `bool`, `string`, and `string[]`.

```csharp
[CommandSpec]
public partial class AnalyzeCommand
{
    [DirectiveSpec(Description = "Enable verbose diagnostics")]
    public bool Verbose { get; set; }

    public int Run() => 0;
}
```

## Handler Methods and Signatures

Supported handler signatures:
- `void Run()` and `int Run()`
- `Task RunAsync()` and `Task<int> RunAsync()`

Optional parameters:
- `CommandRuntimeContext` as the first parameter
- `CancellationToken` as the last parameter
- Any other parameters resolve as services or functions

## Method Commands (Instance Methods)

Public instance methods annotated with `[CommandSpec]` become child commands.

```csharp
[CommandSpec]
public partial class ToolCommand
{
    [CommandSpec(Description = "Clean outputs")]
    public int Clean([OptionSpec] bool force) => 0;
}
```

## Interface-Based Specs

Attributes can live on interfaces when the shape is generated with `[GenerateShapeFor]`.

```csharp
public interface IHasVerbosity
{
    [OptionSpec(Description = "Verbose output")]
    bool Verbose { get; set; }
}

[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
[GenerateShapeFor(typeof(IHasVerbosity))]
[CommandSpec]
public partial class InfoCommand : IHasVerbosity
{
    public bool Verbose { get; set; }
    public int Run() => 0;
}
```

## Naming Rules and Aliases

- Names are auto-generated from type/member names.
- Common suffixes like `Command`, `Option`, and `Argument` are stripped.
- Names default to `kebab-case`.
- Options get long and short forms by default.

Override naming in `[CommandSpec]`:
```csharp
[CommandSpec(
    Name = "init",
    Alias = "i",
    NameAutoGenerate = NameAutoGenerate.None,
    NameCasingConvention = NameCasingConvention.KebabCase)]
public partial class InitializeCommand { }
```

## Requiredness and Arity

Requiredness and arity can be explicit or inferred from nullability and defaults.

```csharp
[CommandSpec]
public partial class DeployCommand
{
    [OptionSpec(Required = true)]
    public string Environment { get; set; } = "";

    [ArgumentSpec(Arity = ArgumentArity.OneOrMore)]
    public string[] Targets { get; set; } = [];
}
```

## Validation and Allowed Values

`ValidationRules` supports common file, directory, path, and URL rules. You can also provide a regex with `ValidationPattern` and a custom `ValidationMessage`.

```csharp
[CommandSpec]
public partial class ScanCommand
{
    [ArgumentSpec(
        ValidationRules = ValidationRules.ExistingFile | ValidationRules.LegalPath,
        ValidationMessage = "Input must be an existing file")]
    public string Input { get; set; } = "";
}
```

## Runtime Creation

Use `CommandRuntime.Factory.Object` for class-based commands and `CommandRuntime.Factory.Function` for function commands.

## Runtime Creation: Class-Based

```csharp
var runtime = CommandRuntime.Factory.Object.Create<RootCommand>(
    settings: null,
    modelRegistry: null,
    modelBuildOptions: null,
    serviceResolver: null);
```

## Runtime Creation: Custom ITypeShapeProvider

```csharp
using PolyType.Abstractions;

var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<RootCommand>();
var provider = shape.Provider;

var runtime = CommandRuntime.Factory.Object.Create(
    commandType: typeof(RootCommand),
    commandTypeShapeProvider: provider,
    settings: null,
    modelRegistry: null,
    modelBuildOptions: null,
    serviceResolver: null);
```

## Runtime Creation: Model-First (Advanced)

```csharp
using PolyType.Abstractions;
using Tenekon.CommandLine.Extensions.PolyType.Model;

var shape = (IObjectTypeShape)TypeShapeResolver.Resolve<RootCommand>();
var provider = shape.Provider;

var registry = new CommandModelRegistry();
var model = registry.Object.GetOrAdd(
    typeof(RootCommand),
    provider,
    new CommandModelBuildOptions { RootParentHandling = RootParentHandling.Ignore });

var runtime = CommandRuntime.Factory.Object.CreateFromModel(
    model,
    settings: null,
    serviceResolver: null);
```

## Runtime Creation: Function-Based

```csharp
using PolyType.Abstractions;

var shape = (IFunctionTypeShape)TypeShapeResolver.Resolve<GreetCommand>();
var provider = shape.Provider;

var runtime = CommandRuntime.Factory.Function.Create(
    commandType: typeof(GreetCommand),
    commandTypeShapeProvider: provider,
    settings: null,
    modelRegistry: null,
    modelBuildOptions: null,
    serviceResolver: null);
```

## Parsing Without Invocation

```csharp
var result = runtime.Parse(args);
if (result.ParseResult.Errors.Count > 0)
{
    // handle errors
}

var instance = result.Bind<RootCommand>();
```

## Binding Results and Helpers

```csharp
var called = result.BindCalled();
var all = result.BindAll();
var isCalled = result.IsCalled<RootCommand>();
var hasRoot = result.Contains<RootCommand>();

if (result.TryGetBinder(typeof(RootCommand), typeof(RootCommand), out var binder))
{
    binder(instance, result.ParseResult);
}
```

## Invocation Options (Per Call)

```csharp
var options = new CommandInvocationOptions
{
    ServiceResolver = new MyServiceResolver(),
    FunctionResolver = new MyFunctionResolver()
};

return runtime.Run(args, options);
```

## CommandRuntimeContext

`CommandRuntimeContext` provides:
- `ParseResult`
- `IsEmptyCommand()`
- `ShowHelp()`
- `ShowHierarchy()`
- `ShowValues()`

```csharp
public int Run(CommandRuntimeContext context)
{
    if (context.IsEmptyCommand())
    {
        context.ShowHelp();
        return 0;
    }

    context.ShowValues();
    return 0;
}
```

## Services and Service Resolution

`ICommandServiceResolver` provides constructor and handler dependencies that are not bound as options, arguments, or directives.

```csharp
public sealed class ServiceProviderResolver(IServiceProvider provider) : ICommandServiceResolver
{
    public bool TryResolve<TService>(out TService? value)
    {
        value = (TService?)provider.GetService(typeof(TService));
        return value is not null;
    }
}

var runtime = CommandRuntime.Factory.Object.Create<RootCommand>(
    settings: null,
    modelRegistry: null,
    modelBuildOptions: null,
    serviceResolver: new ServiceProviderResolver(serviceProvider));
```

## Functions and Function Resolution

Function instances are resolved separately from services. The resolution order is:
- `CommandInvocationOptions.FunctionResolver`
- `CommandRuntime.FunctionRegistry` and `CommandRuntimeSettings.FunctionResolvers`
- `ICommandServiceResolver` if `AllowFunctionResolutionFromServices` is enabled

> [!IMPORTANT]
> Service resolution for functions only happens when `AllowFunctionResolutionFromServices` is enabled.

Register a function instance:
```csharp
runtime.FunctionRegistry.Set<GreetCommand>(name =>
{
    Console.WriteLine($"Hello {name}");
    return 0;
});
```

Custom function resolver:
```csharp
public sealed class MyFunctionResolver : ICommandFunctionResolver
{
    public bool TryResolve<TFunction>(out TFunction value)
    {
        if (typeof(TFunction) == typeof(GreetCommand))
        {
            value = (TFunction)(object)(GreetCommand)(name => 0);
            return true;
        }

        value = default!;
        return false;
    }
}
```

## Settings (CommandRuntimeSettings)

Common settings:
- `EnableDefaultExceptionHandler`
- `ShowHelpOnEmptyCommand`
- `AllowFunctionResolutionFromServices`
- `EnableDiagramDirective`
- `EnableSuggestDirective`
- `EnableEnvironmentVariablesDirective`
- `Output`, `Error`
- `FileSystem` (Advanced)
- `FunctionResolvers`
- `EnablePosixBundling`
- `ResponseFileTokenReplacer`

```csharp
var settings = new CommandRuntimeSettings
{
    ShowHelpOnEmptyCommand = true,
    EnableSuggestDirective = true
};
```

## Response Files and POSIX Bundling

```csharp
using System.CommandLine.Parsing;

var settings = new CommandRuntimeSettings
{
    EnablePosixBundling = true,
    ResponseFileTokenReplacer = static (Token token, out string[]? replacements) =>
    {
        replacements = null;
        return false;
    }
};
```

## Built-In Directives

Configure built-in directives with:
- `EnableDiagramDirective`
- `EnableSuggestDirective`
- `EnableEnvironmentVariablesDirective`

## File System Abstraction (Advanced)

Validation rules use `IFileSystem`. Provide your own implementation if needed.

```csharp
public sealed class InMemoryFileSystem : IFileSystem
{
    public IFileSystemFile File { get; } = new InMemoryFile();
    public IFileSystemDirectory Directory { get; } = new InMemoryDirectory();
    public IFileSystemPath Path { get; } = new InMemoryPath();

    private sealed class InMemoryFile : IFileSystemFile
    {
        public bool FileExists(string path) => false;
    }

    private sealed class InMemoryDirectory : IFileSystemDirectory
    {
        public bool DirectoryExists(string path) => false;
    }

    private sealed class InMemoryPath : IFileSystemPath
    {
        public char[] GetInvalidPathChars() => [];
        public char[] GetInvalidFileNameChars() => [];
    }
}
```

## Trimming and AOT

PolyType source generation allows runtime binding without reflection, making this library trimming-friendly and suitable for Native AOT scenarios.
