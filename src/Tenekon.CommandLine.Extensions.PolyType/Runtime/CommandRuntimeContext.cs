using System.Collections;
using System.CommandLine;
using System.Text;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.Binding;
using Tenekon.CommandLine.Extensions.PolyType.Runtime.Graph;

namespace Tenekon.CommandLine.Extensions.PolyType.Runtime;

public sealed class CommandRuntimeContext
{
    private readonly RuntimeNode _rootNode;
    private readonly CommandRuntimeSettings _settings;

    internal CommandRuntimeContext(
        BindingContext bindingContext,
        ParseResult parseResult,
        CommandRuntimeSettings settings,
        RuntimeNode rootNode,
        ICommandFunctionResolver? functionResolver)
    {
        BindingContext = bindingContext;
        _settings = settings;
        _rootNode = rootNode;
        ParseResult = parseResult;
        FunctionResolver = functionResolver;
    }

    public ParseResult ParseResult { get; }
    internal BindingContext BindingContext { get; }
    internal ICommandFunctionResolver? FunctionResolver { get; }

    public bool IsEmptyCommand()
    {
        return ParseResult.Tokens.Count == 0;
    }

    public void ShowHelp()
    {
        var command = ParseResult.CommandResult?.Command;
        if (command is null) return;
        var config = new ParserConfiguration
        {
            EnablePosixBundling = _settings.EnablePosixBundling
        };

        if (_settings.ResponseFileTokenReplacer is not null)
            config.ResponseFileTokenReplacer = _settings.ResponseFileTokenReplacer;

        var helpResult = command.Parse(["--help"], config);
        helpResult.Invoke(
            new InvocationConfiguration
            {
                EnableDefaultExceptionHandler = _settings.EnableDefaultExceptionHandler,
                Output = _settings.Output,
                Error = _settings.Error
            });
    }

    public void ShowHierarchy()
    {
        WriteHierarchy(_rootNode, indent: 0);
    }

    public void ShowValues()
    {
        if (!BindingContext.TryGetCalledNode(ParseResult, out var node) || node is null) return;

        object? instance = null;
        if (node.DefinitionType is not null)
            instance = BindingContext.Bind(
                ParseResult,
                node.DefinitionType,
                returnEmpty: false,
                cancellationToken: default);

        foreach (var member in node.ValueAccessors)
        {
            var value = member.Getter(instance, ParseResult);
            _settings.Output.WriteLine($"{member.DisplayName} = {FormatValue(value)}");
        }
    }

    private void WriteHierarchy(RuntimeNode descriptor, int indent)
    {
        _settings.Output.WriteLine($"{new string(c: ' ', indent * 2)}{descriptor.DisplayName}");
        foreach (var child in descriptor.Children)
            WriteHierarchy(child, indent + 1);
    }

    private static string FormatValue(object? value)
    {
        if (value is null) return "null";
        if (value is string str) return $"\"{str}\"";
        if (value is IEnumerable enumerable && value is not string)
        {
            var builder = new StringBuilder();
            builder.Append("[");
            var first = true;
            foreach (var item in enumerable)
            {
                if (!first) builder.Append(", ");
                builder.Append(FormatValue(item));
                first = false;
            }

            builder.Append("]");
            return builder.ToString();
        }

        return value.ToString() ?? string.Empty;
    }
}