using System.Collections;
using System.CommandLine;
using System.Text;

namespace Tenekon.CommandLine.Extensions.PolyType;

public sealed class CommandLineContext
{
    private readonly CommandLineBindingContext _bindingContext;
    private readonly CommandDescriptor _rootDescriptor;
    private readonly CommandLineSettings _settings;

    internal CommandLineContext(
        CommandLineBindingContext bindingContext,
        ParseResult parseResult,
        CommandLineSettings settings,
        CommandDescriptor rootDescriptor)
    {
        _bindingContext = bindingContext;
        _settings = settings;
        _rootDescriptor = rootDescriptor;
        ParseResult = parseResult;
    }

    public ParseResult ParseResult { get; }

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
        WriteHierarchy(_rootDescriptor, indent: 0);
    }

    public void ShowValues()
    {
        if (!_bindingContext.TryGetCalledType(ParseResult, out var calledType) || calledType is null)
            return;

        var instance = _bindingContext.Bind(ParseResult, calledType);
        var descriptor = _rootDescriptor.Find(calledType);
        if (descriptor is null) return;

        foreach (var member in descriptor.SpecMembers)
        {
            var value = member.Getter(instance);
            _settings.Output.WriteLine($"{member.DisplayName} = {FormatValue(value)}");
        }
    }

    private void WriteHierarchy(CommandDescriptor descriptor, int indent)
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