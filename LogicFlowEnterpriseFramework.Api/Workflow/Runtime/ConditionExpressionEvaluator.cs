using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LogicFlowEnterpriseFramework.Api.Workflow.Runtime;

internal static partial class ConditionExpressionEvaluator
{
    private static readonly Regex ExpressionRegex = new(
        @"^\s*(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*(?<operator>>=|<=|==|!=|>|<)\s*(?<literal>""[^""]*""|true|false|-?\d+(?:\.\d+)?)\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static bool IsSupportedExpression(string expression) => ExpressionRegex.IsMatch(expression);

    public static bool Evaluate(string expression, IReadOnlyDictionary<string, WorkflowVariableValue> variables)
    {
        var match = ExpressionRegex.Match(expression);
        if (!match.Success)
        {
            throw new WorkflowDefinitionException($"Condition expression '{expression}' is not supported.");
        }

        var variableName = match.Groups["name"].Value;
        var comparisonOperator = match.Groups["operator"].Value;
        var literal = ParseLiteral(match.Groups["literal"].Value);

        if (!variables.TryGetValue(variableName, out var variable))
        {
            throw new WorkflowRuntimeException($"Workflow variable '{variableName}' was not found for condition expression.");
        }

        return Compare(variable, comparisonOperator, literal);
    }

    public static WorkflowVariableValue ParseStoredValue(string? value, string dataType)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new WorkflowVariableValue(null, dataType);
        }

        try
        {
            using var document = JsonDocument.Parse(value);
            var root = document.RootElement;

            return root.ValueKind switch
            {
                JsonValueKind.String => new WorkflowVariableValue(root.GetString(), dataType),
                JsonValueKind.Number when root.TryGetDecimal(out var number) => new WorkflowVariableValue(number, dataType),
                JsonValueKind.True => new WorkflowVariableValue(true, dataType),
                JsonValueKind.False => new WorkflowVariableValue(false, dataType),
                _ => new WorkflowVariableValue(root.GetRawText(), dataType)
            };
        }
        catch (JsonException)
        {
            return new WorkflowVariableValue(value, dataType);
        }
    }

    private static WorkflowVariableValue ParseLiteral(string literal)
    {
        if (literal.StartsWith('"') && literal.EndsWith('"'))
        {
            return new WorkflowVariableValue(literal[1..^1], "String");
        }

        if (bool.TryParse(literal, out var boolean))
        {
            return new WorkflowVariableValue(boolean, "Boolean");
        }

        if (decimal.TryParse(literal, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
        {
            return new WorkflowVariableValue(number, "Number");
        }

        throw new WorkflowDefinitionException($"Literal '{literal}' is not supported.");
    }

    private static bool Compare(WorkflowVariableValue variable, string comparisonOperator, WorkflowVariableValue literal)
    {
        if (variable.Value is decimal variableNumber && literal.Value is decimal literalNumber)
        {
            return comparisonOperator switch
            {
                ">" => variableNumber > literalNumber,
                ">=" => variableNumber >= literalNumber,
                "<" => variableNumber < literalNumber,
                "<=" => variableNumber <= literalNumber,
                "==" => variableNumber == literalNumber,
                "!=" => variableNumber != literalNumber,
                _ => false
            };
        }

        if (variable.Value is bool variableBoolean && literal.Value is bool literalBoolean)
        {
            return comparisonOperator switch
            {
                "==" => variableBoolean == literalBoolean,
                "!=" => variableBoolean != literalBoolean,
                _ => throw new WorkflowDefinitionException("Boolean conditions only support == and !=.")
            };
        }

        if (variable.Value is string variableString && literal.Value is string literalString)
        {
            return comparisonOperator switch
            {
                "==" => string.Equals(variableString, literalString, StringComparison.Ordinal),
                "!=" => !string.Equals(variableString, literalString, StringComparison.Ordinal),
                _ => throw new WorkflowDefinitionException("String conditions only support == and !=.")
            };
        }

        throw new WorkflowDefinitionException("Condition compares incompatible value types.");
    }
}

internal sealed record WorkflowVariableValue(object? Value, string DataType);
