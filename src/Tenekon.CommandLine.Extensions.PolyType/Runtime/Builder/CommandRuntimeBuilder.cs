using System.CommandLine;
using System.CommandLine.Completions;
using System.CommandLine.Help;
using Tenekon.CommandLine.Extensions.PolyType.Model;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.Binding;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.Graph;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.Invocation;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime.Builder;

internal readonly record struct RuntimeBuildResult(RuntimeGraph Graph, BindingContext BindingContext);

internal static class CommandRuntimeBuilder
{
    public static RuntimeBuildResult Build(CommandModel model, CommandRuntimeSettings? settings)
    {
        if (model is null) throw new ArgumentNullException(nameof(model));

        settings ??= CommandRuntimeSettings.Default;

        var bindingRegistry = new BindingRegistry();
        var bindingContext = new BindingContext(bindingRegistry, settings)
        {
            AllowFunctionResolutionFromServices = settings.AllowFunctionResolutionFromServices
        };
        bindingContext.DefaultFunctionResolver = FunctionResolverChain.Create(
            new[] { bindingContext.FunctionRegistry }.Concat(settings.FunctionResolvers));

        var rootNode = model.Graph.RootNode;
        var rootCommand = new RootCommand();
        AddBuiltInSymbols(rootCommand, settings);

        var runtimeRoot = rootNode switch
        {
            CommandObjectNode typeNode => BuildTypeCommand(
                typeNode,
                bindingContext,
                settings,
                parentNamer: null,
                rootCommand),
            CommandFunctionNode functionNode => BuildFunctionCommand(
                functionNode,
                bindingContext,
                settings,
                parentNamer: null,
                rootCommand),
            _ => throw new InvalidOperationException("Unsupported root command node.")
        };

        if (runtimeRoot.Command is not RootCommand)
            throw new InvalidOperationException("Root command is not a RootCommand.");

        bindingContext.Initialize(runtimeRoot);

        var graph = new RuntimeGraph(rootCommand, runtimeRoot);
        return new RuntimeBuildResult(graph, bindingContext);
    }

    private static RuntimeNode BuildTypeCommand(
        CommandObjectNode descriptor,
        BindingContext bindingContext,
        CommandRuntimeSettings settings,
        CommandNamingPolicy? parentNamer,
        RootCommand rootCommand)
    {
        var namer = new CommandNamingPolicy(
            descriptor.Spec.IsNameAutoGenerateSpecified ? descriptor.Spec.NameAutoGenerate : null,
            descriptor.Spec.IsNameCasingConventionSpecified ? descriptor.Spec.NameCasingConvention : null,
            descriptor.Spec.IsNamePrefixConventionSpecified ? descriptor.Spec.NamePrefixConvention : null,
            descriptor.Spec.IsShortFormAutoGenerateSpecified ? descriptor.Spec.ShortFormAutoGenerate : null,
            descriptor.Spec.IsShortFormPrefixConventionSpecified ? descriptor.Spec.ShortFormPrefixConvention : null,
            parentNamer);

        Command command;
        if (descriptor.Parent is null)
        {
            command = rootCommand;
        }
        else
        {
            var commandName = namer.GetCommandName(descriptor.DefinitionType.Name, descriptor.Spec.Name);
            command = new Command(commandName);
            TryAddAliases(command, descriptor.Spec.Alias, descriptor.Spec.Aliases, namer);
            var shortForm = namer.CreateShortForm(commandName, forOption: false);
            if (!string.IsNullOrWhiteSpace(shortForm)) command.Aliases.Add(shortForm);
        }

        if (!string.IsNullOrWhiteSpace(descriptor.Spec.Description)) command.Description = descriptor.Spec.Description;

        command.Hidden = descriptor.Spec.Hidden;
        command.TreatUnmatchedTokensAsErrors = descriptor.Spec.TreatUnmatchedTokensAsErrors;

        bindingContext.CreatorMap[descriptor.DefinitionType] = CreateCreatorFactory(descriptor);

        BuildMembers(command, descriptor, bindingContext, settings, namer, rootCommand);
        AddHandler(command, descriptor, bindingContext, settings);

        var valueAccessors = BuildValueAccessors(descriptor);
        var runtime = RuntimeNode.CreateType(
            descriptor.DefinitionType,
            command,
            valueAccessors,
            descriptor.ParentAccessors);

        foreach (var method in descriptor.MethodChildren.OrderBy(static child => child.Spec.Order)
                     .ThenBy(static child => child.MethodShape.Name, StringComparer.Ordinal))
        {
            var methodRuntime = BuildMethodCommand(method, bindingContext, settings, namer, rootCommand);
            methodRuntime.Parent = runtime;
            runtime.Children.Add(methodRuntime);
            command.Add(methodRuntime.Command);
        }

        foreach (var child in descriptor.Children.OrderBy(static child => child.Spec.Order)
                     .ThenBy(static child => child.DisplayName, StringComparer.Ordinal))
        {
            var childRuntime = child switch
            {
                CommandObjectNode typeChild => BuildTypeCommand(typeChild, bindingContext, settings, namer, rootCommand),
                CommandFunctionNode functionChild => BuildFunctionCommand(
                    functionChild,
                    bindingContext,
                    settings,
                    namer,
                    rootCommand),
                _ => throw new InvalidOperationException("Unsupported child command node.")
            };
            childRuntime.Parent = runtime;
            runtime.Children.Add(childRuntime);
            command.Add(childRuntime.Command);
        }

        return runtime;
    }

    private static RuntimeNode BuildMethodCommand(
        CommandMethodNode methodNode,
        BindingContext bindingContext,
        CommandRuntimeSettings settings,
        CommandNamingPolicy parentNamer,
        RootCommand rootCommand)
    {
        var namer = new CommandNamingPolicy(
            methodNode.Spec.IsNameAutoGenerateSpecified ? methodNode.Spec.NameAutoGenerate : null,
            methodNode.Spec.IsNameCasingConventionSpecified ? methodNode.Spec.NameCasingConvention : null,
            methodNode.Spec.IsNamePrefixConventionSpecified ? methodNode.Spec.NamePrefixConvention : null,
            methodNode.Spec.IsShortFormAutoGenerateSpecified ? methodNode.Spec.ShortFormAutoGenerate : null,
            methodNode.Spec.IsShortFormPrefixConventionSpecified ? methodNode.Spec.ShortFormPrefixConvention : null,
            parentNamer);

        var commandName = namer.GetCommandName(methodNode.MethodShape.Name, methodNode.Spec.Name);
        var command = new Command(commandName);
        TryAddAliases(command, methodNode.Spec.Alias, methodNode.Spec.Aliases, namer);
        var shortForm = namer.CreateShortForm(commandName, forOption: false);
        if (!string.IsNullOrWhiteSpace(shortForm)) command.Aliases.Add(shortForm);

        if (!string.IsNullOrWhiteSpace(methodNode.Spec.Description)) command.Description = methodNode.Spec.Description;

        command.Hidden = methodNode.Spec.Hidden;
        command.TreatUnmatchedTokensAsErrors = methodNode.Spec.TreatUnmatchedTokensAsErrors;

        var parameterBindings = BuildMethodParameters(
            methodNode,
            command,
            settings,
            namer,
            rootCommand,
            out var valueAccessors);

        var handler = CommandMethodHandlerFactory.TryCreateHandler(
            methodNode.MethodShape,
            parameterBindings,
            bindingContext,
            settings);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            return await handler.InvokeAsync(parseResult, bindingContext.CurrentServiceResolver, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
        });

        var runtime = RuntimeNode.CreateMethod(methodNode.MethodShape, command, valueAccessors);

        foreach (var child in methodNode.Children.OrderBy(static child => child.Spec.Order)
                     .ThenBy(static child => child.DisplayName, StringComparer.Ordinal))
        {
            var childRuntime = child switch
            {
                CommandObjectNode typeChild => BuildTypeCommand(typeChild, bindingContext, settings, namer, rootCommand),
                CommandFunctionNode functionChild => BuildFunctionCommand(
                    functionChild,
                    bindingContext,
                    settings,
                    namer,
                    rootCommand),
                _ => throw new InvalidOperationException("Unsupported child command node.")
            };
            childRuntime.Parent = runtime;
            runtime.Children.Add(childRuntime);
            command.Add(childRuntime.Command);
        }

        return runtime;
    }

    private static RuntimeNode BuildFunctionCommand(
        CommandFunctionNode functionNode,
        BindingContext bindingContext,
        CommandRuntimeSettings settings,
        CommandNamingPolicy? parentNamer,
        RootCommand rootCommand)
    {
        var namer = new CommandNamingPolicy(
            functionNode.Spec.IsNameAutoGenerateSpecified ? functionNode.Spec.NameAutoGenerate : null,
            functionNode.Spec.IsNameCasingConventionSpecified ? functionNode.Spec.NameCasingConvention : null,
            functionNode.Spec.IsNamePrefixConventionSpecified ? functionNode.Spec.NamePrefixConvention : null,
            functionNode.Spec.IsShortFormAutoGenerateSpecified ? functionNode.Spec.ShortFormAutoGenerate : null,
            functionNode.Spec.IsShortFormPrefixConventionSpecified ? functionNode.Spec.ShortFormPrefixConvention : null,
            parentNamer);

        Command command;
        if (functionNode.Parent is null)
        {
            command = rootCommand;
        }
        else
        {
            var commandName = namer.GetCommandName(functionNode.FunctionType.Name, functionNode.Spec.Name);
            command = new Command(commandName);
            TryAddAliases(command, functionNode.Spec.Alias, functionNode.Spec.Aliases, namer);
            var shortForm = namer.CreateShortForm(commandName, forOption: false);
            if (!string.IsNullOrWhiteSpace(shortForm)) command.Aliases.Add(shortForm);
        }

        if (!string.IsNullOrWhiteSpace(functionNode.Spec.Description))
            command.Description = functionNode.Spec.Description;

        command.Hidden = functionNode.Spec.Hidden;
        command.TreatUnmatchedTokensAsErrors = functionNode.Spec.TreatUnmatchedTokensAsErrors;

        var parameterBindings = BuildFunctionParameters(
            functionNode,
            command,
            settings,
            namer,
            rootCommand,
            out var valueAccessors);

        var handler = CommandFunctionHandlerFactory.TryCreateHandler(
            functionNode.FunctionShape,
            parameterBindings,
            bindingContext,
            settings);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            return await handler.InvokeAsync(parseResult, bindingContext.CurrentServiceResolver, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
        });

        var runtime = RuntimeNode.CreateFunction(
            functionNode.FunctionType,
            functionNode.FunctionShape,
            command,
            valueAccessors);

        foreach (var child in functionNode.Children.OrderBy(static child => child.Spec.Order)
                     .ThenBy(static child => child.DisplayName, StringComparer.Ordinal))
        {
            var childRuntime = child switch
            {
                CommandObjectNode typeChild => BuildTypeCommand(typeChild, bindingContext, settings, namer, rootCommand),
                CommandFunctionNode functionChild => BuildFunctionCommand(
                    functionChild,
                    bindingContext,
                    settings,
                    namer,
                    rootCommand),
                _ => throw new InvalidOperationException("Unsupported child command node.")
            };
            childRuntime.Parent = runtime;
            runtime.Children.Add(childRuntime);
            command.Add(childRuntime.Command);
        }

        return runtime;
    }

    private static void BuildMembers(
        Command command,
        CommandObjectNode descriptor,
        BindingContext bindingContext,
        CommandRuntimeSettings settings,
        CommandNamingPolicy namer,
        RootCommand rootCommand)
    {
        var targets = new List<Type> { descriptor.DefinitionType };
        targets.AddRange(descriptor.InterfaceTargets);

        foreach (var entry in descriptor.SpecEntries)
        {
            if (entry.Option is not null)
            {
                BuildOption(entry, command, bindingContext, settings, namer, targets, descriptor.DefinitionType);
                continue;
            }

            if (entry.Argument is not null)
            {
                BuildArgument(entry, command, bindingContext, settings, namer, targets, descriptor.DefinitionType);
                continue;
            }

            if (entry.Directive is not null)
                BuildDirective(entry, rootCommand, bindingContext, namer, targets, descriptor.DefinitionType);
        }
    }

    private static void BuildOption(
        CommandObjectNode.SpecEntry entry,
        Command command,
        BindingContext bindingContext,
        CommandRuntimeSettings settings,
        CommandNamingPolicy namer,
        IReadOnlyList<Type> targets,
        Type definitionType)
    {
        var builder = new OptionMemberBuilder(
            entry.Property,
            entry.ValueProperty,
            entry.Option!,
            namer,
            settings.FileSystem);
        var result = builder.Build();
        if (result is null) return;

        command.Add((Option)result.Symbol);
        AddBinders(bindingContext, targets, result.Binder, entry.OwnerType, definitionType);
    }

    private static void BuildArgument(
        CommandObjectNode.SpecEntry entry,
        Command command,
        BindingContext bindingContext,
        CommandRuntimeSettings settings,
        CommandNamingPolicy namer,
        IReadOnlyList<Type> targets,
        Type definitionType)
    {
        var builder = new ArgumentMemberBuilder(
            entry.Property,
            entry.ValueProperty,
            entry.Argument!,
            namer,
            settings.FileSystem);
        var result = builder.Build();
        if (result is null) return;

        command.Add((Argument)result.Symbol);
        AddBinders(bindingContext, targets, result.Binder, entry.OwnerType, definitionType);
    }

    private static void BuildDirective(
        CommandObjectNode.SpecEntry entry,
        RootCommand rootCommand,
        BindingContext bindingContext,
        CommandNamingPolicy namer,
        IReadOnlyList<Type> targets,
        Type definitionType)
    {
        var builder = new DirectiveMemberBuilder(entry.Property, entry.Directive!, namer);
        var result = builder.Build();
        if (result is null) return;

        rootCommand.Add(result.Directive);
        AddBinders(bindingContext, targets, result.Binder, entry.OwnerType, definitionType);
    }

    private static void AddHandler(
        Command command,
        CommandObjectNode descriptor,
        BindingContext bindingContext,
        CommandRuntimeSettings settings)
    {
        var handler = CommandHandlerFactory.TryCreateHandler(descriptor.Shape, bindingContext, settings);
        if (handler is null) return;

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            return await handler.InvokeAsync(parseResult, bindingContext.CurrentServiceResolver, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
        });
    }

    private static MethodParameterBindingInfo[] BuildMethodParameters(
        CommandMethodNode methodNode,
        Command command,
        CommandRuntimeSettings settings,
        CommandNamingPolicy namer,
        RootCommand rootCommand,
        out IReadOnlyList<RuntimeValueAccessor> valueAccessors)
    {
        var parameters = methodNode.MethodShape.Parameters;
        var bindings = new MethodParameterBindingInfo[parameters.Count];
        var accessors = new List<RuntimeValueAccessor>();
        var specMap = methodNode.ParameterSpecs.ToDictionary(entry => entry.Parameter.Position);

        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            var parameterType = parameter.ParameterType.Type;

            if (parameterType == typeof(CommandRuntimeContext))
            {
                if (i != 0)
                    throw new InvalidOperationException(
                        $"Command method '{methodNode.ParentType.DefinitionType.FullName}.{methodNode.MethodShape.Name}' cannot use CommandRuntimeContext outside the first parameter.");

                if (specMap.ContainsKey(parameter.Position))
                    throw new InvalidOperationException(
                        $"Command method '{methodNode.ParentType.DefinitionType.FullName}.{methodNode.MethodShape.Name}' cannot apply CLI specs to CommandRuntimeContext.");

                bindings[i] = new MethodParameterBindingInfo(MethodParameterBindingKind.Context, Symbol: null);
                continue;
            }

            if (parameterType == typeof(CancellationToken))
            {
                if (i != parameters.Count - 1)
                    throw new InvalidOperationException(
                        $"Command method '{methodNode.ParentType.DefinitionType.FullName}.{methodNode.MethodShape.Name}' cannot use CancellationToken outside the last parameter.");

                if (specMap.ContainsKey(parameter.Position))
                    throw new InvalidOperationException(
                        $"Command method '{methodNode.ParentType.DefinitionType.FullName}.{methodNode.MethodShape.Name}' cannot apply CLI specs to CancellationToken.");

                bindings[i] = new MethodParameterBindingInfo(
                    MethodParameterBindingKind.CancellationToken,
                    Symbol: null);
                continue;
            }

            if (!specMap.TryGetValue(parameter.Position, out var specEntry))
            {
                bindings[i] = new MethodParameterBindingInfo(MethodParameterBindingKind.Service, Symbol: null);
                continue;
            }

            if (specEntry.Option is not null)
            {
                var builder = new OptionParameterBuilder(parameter, specEntry.Option, namer, settings.FileSystem);
                var result = builder.Build();
                if (result is null)
                    throw new InvalidOperationException(
                        $"Unable to build option for '{methodNode.ParentType.DefinitionType.FullName}.{methodNode.MethodShape.Name}.{parameter.Name}'.");

                var buildResult = result.Value;
                command.Add((Option)buildResult.Symbol);
                accessors.Add(buildResult.Accessor);
                bindings[i] = new MethodParameterBindingInfo(MethodParameterBindingKind.Option, buildResult.Symbol);
                continue;
            }

            if (specEntry.Argument is not null)
            {
                var builder = new ArgumentParameterBuilder(parameter, specEntry.Argument, namer, settings.FileSystem);
                var result = builder.Build();
                if (result is null)
                    throw new InvalidOperationException(
                        $"Unable to build argument for '{methodNode.ParentType.DefinitionType.FullName}.{methodNode.MethodShape.Name}.{parameter.Name}'.");

                var buildResult = result.Value;
                command.Add((Argument)buildResult.Symbol);
                accessors.Add(buildResult.Accessor);
                bindings[i] = new MethodParameterBindingInfo(MethodParameterBindingKind.Argument, buildResult.Symbol);
                continue;
            }

            if (specEntry.Directive is not null)
            {
                var builder = new DirectiveParameterBuilder(parameter, specEntry.Directive, namer);
                var result = builder.Build();
                if (result is null)
                    throw new InvalidOperationException(
                        $"Unable to build directive for '{methodNode.ParentType.DefinitionType.FullName}.{methodNode.MethodShape.Name}.{parameter.Name}'.");

                var buildResult = result.Value;
                rootCommand.Add(buildResult.Directive);
                accessors.Add(buildResult.Accessor);
                bindings[i] = new MethodParameterBindingInfo(
                    MethodParameterBindingKind.Directive,
                    buildResult.Directive);
                continue;
            }

            bindings[i] = new MethodParameterBindingInfo(MethodParameterBindingKind.Service, Symbol: null);
        }

        valueAccessors = accessors;
        return bindings;
    }

    private static MethodParameterBindingInfo[] BuildFunctionParameters(
        CommandFunctionNode functionNode,
        Command command,
        CommandRuntimeSettings settings,
        CommandNamingPolicy namer,
        RootCommand rootCommand,
        out IReadOnlyList<RuntimeValueAccessor> valueAccessors)
    {
        var parameters = functionNode.FunctionShape.Parameters;
        var bindings = new MethodParameterBindingInfo[parameters.Count];
        var accessors = new List<RuntimeValueAccessor>();
        var specMap = functionNode.ParameterSpecs.ToDictionary(entry => entry.Parameter.Position);
        var displayName = functionNode.FunctionType.FullName ?? functionNode.FunctionType.Name;

        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            var parameterType = parameter.ParameterType.Type;

            if (parameterType == typeof(CommandRuntimeContext))
            {
                if (i != 0)
                    throw new InvalidOperationException(
                        $"Command function '{displayName}' cannot use CommandRuntimeContext outside the first parameter.");

                if (specMap.ContainsKey(parameter.Position))
                    throw new InvalidOperationException(
                        $"Command function '{displayName}' cannot apply CLI specs to CommandRuntimeContext.");

                bindings[i] = new MethodParameterBindingInfo(MethodParameterBindingKind.Context, Symbol: null);
                continue;
            }

            if (parameterType == typeof(CancellationToken))
            {
                if (i != parameters.Count - 1)
                    throw new InvalidOperationException(
                        $"Command function '{displayName}' cannot use CancellationToken outside the last parameter.");

                if (specMap.ContainsKey(parameter.Position))
                    throw new InvalidOperationException(
                        $"Command function '{displayName}' cannot apply CLI specs to CancellationToken.");

                bindings[i] = new MethodParameterBindingInfo(
                    MethodParameterBindingKind.CancellationToken,
                    Symbol: null);
                continue;
            }

            if (!specMap.TryGetValue(parameter.Position, out var specEntry))
            {
                bindings[i] = new MethodParameterBindingInfo(MethodParameterBindingKind.Service, Symbol: null);
                continue;
            }

            if (specEntry.Option is not null)
            {
                var builder = new OptionParameterBuilder(parameter, specEntry.Option, namer, settings.FileSystem);
                var result = builder.Build();
                if (result is null)
                    throw new InvalidOperationException(
                        $"Unable to build option for '{displayName}.{parameter.Name}'.");

                var buildResult = result.Value;
                command.Add((Option)buildResult.Symbol);
                accessors.Add(buildResult.Accessor);
                bindings[i] = new MethodParameterBindingInfo(MethodParameterBindingKind.Option, buildResult.Symbol);
                continue;
            }

            if (specEntry.Argument is not null)
            {
                var builder = new ArgumentParameterBuilder(parameter, specEntry.Argument, namer, settings.FileSystem);
                var result = builder.Build();
                if (result is null)
                    throw new InvalidOperationException(
                        $"Unable to build argument for '{displayName}.{parameter.Name}'.");

                var buildResult = result.Value;
                command.Add((Argument)buildResult.Symbol);
                accessors.Add(buildResult.Accessor);
                bindings[i] = new MethodParameterBindingInfo(MethodParameterBindingKind.Argument, buildResult.Symbol);
                continue;
            }

            if (specEntry.Directive is not null)
            {
                var builder = new DirectiveParameterBuilder(parameter, specEntry.Directive, namer);
                var result = builder.Build();
                if (result is null)
                    throw new InvalidOperationException(
                        $"Unable to build directive for '{displayName}.{parameter.Name}'.");

                var buildResult = result.Value;
                rootCommand.Add(buildResult.Directive);
                accessors.Add(buildResult.Accessor);
                bindings[i] = new MethodParameterBindingInfo(
                    MethodParameterBindingKind.Directive,
                    buildResult.Directive);
                continue;
            }

            bindings[i] = new MethodParameterBindingInfo(MethodParameterBindingKind.Service, Symbol: null);
        }

        valueAccessors = accessors;
        return bindings;
    }

    private static IReadOnlyList<RuntimeValueAccessor> BuildValueAccessors(CommandObjectNode descriptor)
    {
        if (descriptor.SpecMembers.Count == 0) return [];
        var accessors = new List<RuntimeValueAccessor>(descriptor.SpecMembers.Count);
        foreach (var member in descriptor.SpecMembers)
            accessors.Add(new RuntimeValueAccessor(member.DisplayName, (instance, _) => member.Getter(instance!)));

        return accessors;
    }

    private static Func<BindingContext, ParseResult, ICommandServiceResolver?, CancellationToken, object>
        CreateCreatorFactory(CommandObjectNode descriptor)
    {
        var constructor = descriptor.Shape.Constructor;
        if (constructor is null)
            throw new InvalidOperationException(
                $"Type '{descriptor.DefinitionType.FullName}' does not expose a constructor shape.");

        var factory = new ConstructorFactory();
        if (constructor.Accept(factory) is not
            Func<BindingContext, ParseResult, ICommandServiceResolver?, CancellationToken, object> creator)
            throw new InvalidOperationException(
                $"Unable to create factory for '{descriptor.DefinitionType.FullName}'.");

        return creator;
    }

    private static void AddBuiltInSymbols(RootCommand rootCommand, CommandRuntimeSettings settings)
    {
        rootCommand.Add(new HelpOption());
        rootCommand.Add(new VersionOption());

        if (settings.EnableSuggestDirective) rootCommand.Add(new SuggestDirective());
        if (settings.EnableDiagramDirective) rootCommand.Add(new DiagramDirective());
        if (settings.EnableEnvironmentVariablesDirective) rootCommand.Add(new EnvironmentVariablesDirective());
    }

    private static void TryAddAliases(Command command, string? alias, string[]? aliases, CommandNamingPolicy namer)
    {
        if (!string.IsNullOrWhiteSpace(alias))
        {
            var normalized = namer.NormalizeOptionAlias(alias, shortForm: false);
            namer.AddAlias(normalized);
            command.Aliases.Add(normalized);
        }

        if (aliases is not null)
            foreach (var entry in aliases)
            {
                if (string.IsNullOrWhiteSpace(entry)) continue;
                var normalized = namer.NormalizeOptionAlias(entry, shortForm: false);
                namer.AddAlias(normalized);
                command.Aliases.Add(normalized);
            }
    }

    private static void AddBinders(
        BindingContext bindingContext,
        IReadOnlyList<Type> targets,
        Action<object, ParseResult> binder,
        Type ownerType,
        Type definitionType)
    {
        foreach (var target in targets)
        {
            if (!ownerType.IsAssignableFrom(target)) continue;
            var key = new BinderKey(definitionType, target);
            if (bindingContext.BinderMap.TryGetValue(key, out var existing))
                bindingContext.BinderMap[key] = (instance, parseResult) =>
                {
                    existing(instance, parseResult);
                    binder(instance, parseResult);
                };
            else
                bindingContext.BinderMap[key] = binder;
        }
    }
}