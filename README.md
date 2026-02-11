# Tenekon.CommandLine.Extensions.PolyType

[![Build](https://github.com/tenekon/Tenekon.CommandLine.Extensions.PolyType/actions/workflows/coverage.yml/badge.svg?branch=main)](https://github.com/tenekon/Tenekon.CommandLine.Extensions.PolyType/actions/workflows/coverage.yml)
[![NuGet](https://img.shields.io/nuget/v/Tenekon.CommandLine.Extensions.PolyType.svg)](https://www.nuget.org/packages/Tenekon.CommandLine.Extensions.PolyType)
[![Codecov](https://codecov.io/gh/tenekon/Tenekon.CommandLine.Extensions.PolyType/branch/main/graph/badge.svg)](https://codecov.io/gh/tenekon/Tenekon.CommandLine.Extensions.PolyType)
[![License](https://img.shields.io/github/license/tenekon/Tenekon.CommandLine.Extensions.PolyType.svg)](LICENSE)

System.CommandLine is a powerful parser, but composing a class-based CLI can get verbose.
Tenekon.CommandLine.Extensions.PolyType provides a declarative, attribute-driven layer on top of
System.CommandLine using PolyType for fast, strongly-typed binding with no runtime reflection.

## Getting started

Install the package:
```console
dotnet add package Tenekon.CommandLine.Extensions.PolyType
```

Add PolyType so shapes are generated for your command types:
```console
dotnet add package PolyType
```

## Prerequisites

- Any project that can reference `netstandard2.0` (the package also ships `net10.0`).
- Use the PolyType source generator (`[GenerateShape]`) for command types.
- Command types must be `partial` (enforced by diagnostics).

## Usage (class-based model)

In `Program.cs`:
```csharp
using Tenekon.CommandLine.Extensions.PolyType;

var app = CommandLineApp.CreateFromType<RootCommand>();
return app.Run(args);
```

Create a command class:
```csharp
using PolyType;
using PolyType.SourceGenModel;
using Tenekon.CommandLine.Extensions.PolyType;

[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
[CommandSpec(Description = "A root command")]
public partial class RootCommand
{
    [OptionSpec(Description = "Greeting target")]
    public string Name { get; set; } = "world";

    [ArgumentSpec(Description = "Input file")]
    public string? File { get; set; }

    public int Run(CommandLineContext context)
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

Async handler:
```csharp
public Task<int> RunAsync(CommandLineContext context, CancellationToken token)
{
    // ...
    return Task.FromResult(0);
}
```

### Summary
- Mark command classes with `[CommandSpec]`.
- Mark properties with `[OptionSpec]` or `[ArgumentSpec]`.
- Add `Run`/`RunAsync` handler methods.
- Use `CommandLineApp.CreateFromType<TCommand>()` to run.

### Handler signatures

Supported signatures:
- `void Run()` / `int Run()`
- `Task RunAsync()` / `Task<int> RunAsync()`

Optional parameters:
- `CommandLineContext` as the first parameter
- `CancellationToken` as the last parameter
- Any other parameters are resolved from `IServiceProvider`

### Parsing without invocation

```csharp
var app = CommandLineApp.CreateFromType<RootCommand>();
var result = app.Parse(args);

if (result.ParseResult.Errors.Count > 0)
{
    // handle errors
}

var instance = result.Bind<RootCommand>();
```

### Inspecting results

`CommandLineResult` exposes helpers for more advanced flows:
- `BindAll()` / `BindCalled()`
- `TryBindCalled(out object?)`
- `Contains<T>()` / `IsCalled<T>()`
- `TryGetCalledType(out Type?)`
- `TryGetBinder(Type commandType, Type targetType, out Action<object, ParseResult>?)`

## CommandLineApp creation

You can create an app using a custom shape provider:
```csharp
var app = CommandLineApp.CreateFromProvider(
    commandType: typeof(RootCommand),
    commandTypeShapeProvider: provider,
    settings: null,
    serviceProvider: null);
```

## Help output

Help output comes from System.CommandLine and is generated automatically from your specs.
The header uses assembly metadata, the description comes from `[CommandSpec].Description`.

You can also call helpers on `CommandLineContext`:
- `ShowHelp()`
- `ShowHierarchy()`
- `ShowValues()`
- `IsEmptyCommand()`

## Naming conventions

By default, names are auto-generated:
- Command/option/argument names are generated from class/property names.
- Common suffixes (`Command`, `Option`, `Argument`, `Directive`, etc.) are stripped.
- Names are converted to `kebab-case`.
- Options get `--long` and short aliases like `-o1`.

You can override naming via `[CommandSpec]`:
- `Name`, `Alias`, `Aliases`
- `NameAutoGenerate`, `NameCasingConvention`, `NamePrefixConvention`
- `ShortFormAutoGenerate`, `ShortFormPrefixConvention`
- `Order`, `Hidden`, `TreatUnmatchedTokensAsErrors`

## Command composition

Use parent/child relationships or nested types:
```csharp
[CommandSpec(Description = "Root")]
public partial class RootCommand
{
    [CommandSpec(Description = "Child command")]
    public partial class ChildCommand
    {
        public void Run() { }
    }
}
```

Or link by type:
```csharp
[CommandSpec(Description = "Root", Children = new[] { typeof(ChildCommand) })]
public partial class RootCommand { }

[CommandSpec(Parent = typeof(RootCommand), Description = "Child")]
public partial class ChildCommand { }
```

## Options and arguments

`[OptionSpec]` supports:
- `Name`, `Alias`, `Aliases`, `Description`, `HelpName`, `Hidden`, `Order`
- `Recursive`, `AllowMultipleArgumentsPerToken`
- `AllowedValues`
- `Required`, `Arity`
- `ValidationRules`, `ValidationPattern`, `ValidationMessage`

`[ArgumentSpec]` supports:
- `Name`, `Description`, `HelpName`, `Hidden`, `Order`
- `AllowedValues`
- `Required`, `Arity`
- `ValidationRules`, `ValidationPattern`, `ValidationMessage`

If `Required` isn’t specified, it’s inferred from nullability and default values.
`Arity` can be forced via the attribute or inferred for required arguments.

## Validation and allowed values

`ValidationRules` includes file/path/URL rules and can be combined with bitwise OR.
Use `ValidationPattern` and `ValidationMessage` for custom regex validation.

## Directives

Define custom directives via `[DirectiveSpec]`:
```csharp
[DirectiveSpec]
public bool Debug { get; set; }
```

Supported directive property types:
- `bool`
- `string`
- `string[]`

`[DirectiveSpec]` supports:
- `Name`, `Description`, `Hidden`, `Order`

Built-in directives are configurable in `CommandLineSettings`:
- `EnableDiagramDirective`
- `EnableSuggestDirective`
- `EnableEnvironmentVariablesDirective`

## Response files

System.CommandLine response files are supported. You can customize token replacement via:
`CommandLineSettings.ResponseFileTokenReplacer`.

## Dependency injection

Provide a service provider for constructor injection and handler parameters:
```csharp
var services = new ServiceCollection();
services.AddSingleton<MyService>();
var provider = services.BuildServiceProvider();

var app = CommandLineApp.CreateFromType<RootCommand>(
    settings: null,
    serviceProvider: provider,
    commandTypeShapeProvider: null);

return app.Run(args);
```

Per-invocation override:
```csharp
var config = new CommandInvocationConfiguration { ServiceProvider = provider };
return app.Run(args, config);
```

## Interface-based specs

Option/argument specs can live on interfaces. Use PolyType’s `[GenerateShapeFor]`
to generate shapes for those interfaces, and the attributes will be picked up from the interface definition.

## Settings

`CommandLineSettings` controls:
- default exception handler
- help on empty commands
- built-in directives
- response file token replacement
- output/error writers
- POSIX bundling

## Trimming and AOT

This library is reflection-free at runtime when you use PolyType source-generated shapes,
which makes it friendly for trimming and Native AOT deployments.
