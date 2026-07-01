using System.Text.Json;
using System.Xml;

namespace LogicFlowEnterpriseFramework.Api.Workflow.Runtime;

internal sealed class WorkflowDefinitionDocument
{
    private static readonly HashSet<string> SupportedNodeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "start",
        "approval",
        "usertask",
        "condition",
        "timer",
        "delay",
        "servicetask",
        "notification",
        "end"
    };

    private static readonly HashSet<string> SupportedAssignmentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "User",
        "Group",
        "Role"
    };

    private WorkflowDefinitionDocument(
        IReadOnlyDictionary<string, WorkflowNode> nodes,
        IReadOnlyList<WorkflowEdge> edges)
    {
        Nodes = nodes;
        Edges = edges;
    }

    public IReadOnlyDictionary<string, WorkflowNode> Nodes { get; }
    public IReadOnlyList<WorkflowEdge> Edges { get; }

    public static WorkflowDefinitionDocument Parse(string definitionJson)
    {
        var validation = Validate(definitionJson);
        if (!validation.IsValid)
        {
            throw new WorkflowDefinitionException(string.Join(" ", validation.Errors));
        }

        return validation.Document!;
    }

    public static WorkflowDefinitionValidationResult Validate(string? definitionJson)
    {
        if (string.IsNullOrWhiteSpace(definitionJson))
        {
            return WorkflowDefinitionValidationResult.Invalid("Workflow definition JSON is required.");
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(definitionJson);
        }
        catch (JsonException exception)
        {
            return WorkflowDefinitionValidationResult.Invalid($"Workflow definition JSON is invalid: {exception.Message}");
        }

        using (document)
        {
            var errors = new List<string>();
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return WorkflowDefinitionValidationResult.Invalid("Workflow definition root must be a JSON object.");
            }

            if (!root.TryGetProperty("nodes", out var nodesElement) || nodesElement.ValueKind != JsonValueKind.Array)
            {
                errors.Add("Workflow definition requires a nodes array.");
            }

            if (!root.TryGetProperty("edges", out var edgesElement) || edgesElement.ValueKind != JsonValueKind.Array)
            {
                errors.Add("Workflow definition requires an edges array.");
            }

            if (errors.Count > 0)
            {
                return WorkflowDefinitionValidationResult.Invalid(errors);
            }

            var nodes = ParseNodes(nodesElement, errors);
            var edges = ParseEdges(edgesElement, nodes, errors);

            ValidateNodeRules(nodes, edges, errors);
            ValidateGraphRules(nodes, edges, errors);

            return errors.Count == 0
                ? WorkflowDefinitionValidationResult.Valid(new WorkflowDefinitionDocument(nodes, edges))
                : WorkflowDefinitionValidationResult.Invalid(errors);
        }
    }

    public WorkflowNode GetStartNode() => Nodes.Values.Single(x => x.Type == "start");

    public WorkflowNode? GetSingleNextNode(string nodeId)
    {
        var outgoing = Edges.Where(x => string.Equals(x.From, nodeId, StringComparison.OrdinalIgnoreCase)).ToList();
        if (outgoing.Count == 0)
        {
            return null;
        }

        if (outgoing.Count > 1)
        {
            throw new WorkflowDefinitionException($"Node '{nodeId}' has multiple outgoing edges.");
        }

        return Nodes[outgoing[0].To];
    }

    public WorkflowNode GetConditionNextNode(string nodeId, bool outcome)
    {
        var expectedOutcome = outcome ? "true" : "false";
        var edge = Edges.SingleOrDefault(x =>
            string.Equals(x.From, nodeId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.Outcome, expectedOutcome, StringComparison.OrdinalIgnoreCase));

        if (edge is null)
        {
            throw new WorkflowDefinitionException($"Condition node '{nodeId}' is missing {expectedOutcome} outcome edge.");
        }

        return Nodes[edge.To];
    }

    private static Dictionary<string, WorkflowNode> ParseNodes(JsonElement nodesElement, List<string> errors)
    {
        var nodes = new Dictionary<string, WorkflowNode>(StringComparer.OrdinalIgnoreCase);
        foreach (var nodeElement in nodesElement.EnumerateArray())
        {
            if (nodeElement.ValueKind != JsonValueKind.Object)
            {
                errors.Add("Every node must be a JSON object.");
                continue;
            }

            var id = ReadRequiredString(nodeElement, "id", errors);
            var type = ReadRequiredString(nodeElement, "type", errors)?.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(type))
            {
                continue;
            }

            if (!nodes.TryAdd(id, new WorkflowNode(
                id,
                type,
                ReadOptionalString(nodeElement, "name"),
                ReadOptionalGuid(nodeElement, "assignedToUserId"),
                ReadOptionalGuid(nodeElement, "assignedToGroupId"),
                ReadOptionalGuid(nodeElement, "assignedToRoleId"),
                ReadOptionalString(nodeElement, "assignmentType"),
                ReadOptionalString(nodeElement, "assignmentExpression"),
                ReadOptionalString(nodeElement, "expression"),
                ReadOptionalInt(nodeElement, "dueInHours"),
                ReadOptionalString(nodeElement, "timerType") ?? ReadOptionalString(nodeElement, "waitType"),
                ReadOptionalString(nodeElement, "timerExpression") ?? ReadOptionalString(nodeElement, "waitExpression"),
                ReadOptionalString(nodeElement, "processMode"),
                ReadOptionalString(nodeElement, "serviceKey") ?? ReadOptionalString(nodeElement, "processKey"),
                ReadOptionalString(nodeElement, "externalApiEndpointId"),
                ReadOptionalString(nodeElement, "inputMapping"),
                ReadOptionalString(nodeElement, "outputMapping"),
                ReadOptionalString(nodeElement, "targetVariable"),
                ReadOptionalString(nodeElement, "operation"),
                ReadOptionalString(nodeElement, "valueExpression"),
                ReadOptionalString(nodeElement, "retryPolicy"),
                ReadOptionalString(nodeElement, "notificationKey"),
                ReadOptionalString(nodeElement, "channel"),
                ReadOptionalString(nodeElement, "templateKey"),
                ReadOptionalJson(nodeElement, "metadata"))))
            {
                errors.Add($"Duplicate workflow node id '{id}'.");
            }
        }

        return nodes;
    }

    private static List<WorkflowEdge> ParseEdges(
        JsonElement edgesElement,
        IReadOnlyDictionary<string, WorkflowNode> nodes,
        List<string> errors)
    {
        var edges = new List<WorkflowEdge>();
        foreach (var edgeElement in edgesElement.EnumerateArray())
        {
            if (edgeElement.ValueKind != JsonValueKind.Object)
            {
                errors.Add("Every edge must be a JSON object.");
                continue;
            }

            var from = ReadRequiredString(edgeElement, "from", errors);
            var to = ReadRequiredString(edgeElement, "to", errors);
            var outcome = ReadOptionalString(edgeElement, "outcome")?.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
            {
                continue;
            }

            if (!nodes.ContainsKey(from))
            {
                errors.Add($"Edge references missing from node '{from}'.");
            }

            if (!nodes.ContainsKey(to))
            {
                errors.Add($"Edge references missing to node '{to}'.");
            }

            edges.Add(new WorkflowEdge(from, to, outcome));
        }

        return edges;
    }

    private static void ValidateNodeRules(IReadOnlyDictionary<string, WorkflowNode> nodes, IReadOnlyList<WorkflowEdge> edges, List<string> errors)
    {
        if (nodes.Values.Count(x => x.Type == "start") != 1)
        {
            errors.Add("Workflow definition must contain exactly one start node.");
        }

        if (!nodes.Values.Any(x => x.Type == "end"))
        {
            errors.Add("Workflow definition must contain at least one end node.");
        }

        foreach (var node in nodes.Values)
        {
            if (!SupportedNodeTypes.Contains(node.Type))
            {
                errors.Add($"Node '{node.Id}' has unsupported type '{node.Type}'.");
            }

            if (node.Type is "approval" or "usertask")
            {
                var hasAssignee = node.AssignedToUserId.HasValue || node.AssignedToGroupId.HasValue || node.AssignedToRoleId.HasValue;
                if (!hasAssignee)
                {
                    errors.Add($"User task node '{node.Id}' requires assignedToUserId, assignedToGroupId, or assignedToRoleId.");
                }

                if (!string.IsNullOrWhiteSpace(node.AssignmentType) && !SupportedAssignmentTypes.Contains(node.AssignmentType))
                {
                    errors.Add($"User task node '{node.Id}' has unsupported assignmentType '{node.AssignmentType}'.");
                }
            }

            if (node.Type == "condition" && string.IsNullOrWhiteSpace(node.Expression))
            {
                errors.Add($"Condition node '{node.Id}' requires expression.");
            }
            else if (node.Type == "condition" && !ConditionExpressionEvaluator.IsSupportedExpression(node.Expression!))
            {
                errors.Add($"Condition node '{node.Id}' has unsupported expression '{node.Expression}'.");
            }

            if (node.Type is "timer" or "delay")
            {
                var timerKind = string.IsNullOrWhiteSpace(node.TimerType) ? "duration" : node.TimerType;
                if (node.DueInHours is <= 0)
                {
                    errors.Add($"Timer node '{node.Id}' must have dueInHours greater than zero.");
                }

                if (!string.IsNullOrWhiteSpace(node.TimerExpression)
                    && !IsSupportedTimerExpression(node.TimerExpression!))
                {
                    errors.Add($"Timer node '{node.Id}' has unsupported timerExpression '{node.TimerExpression}'.");
                }

                if (string.Equals(timerKind, "expression", StringComparison.OrdinalIgnoreCase)
                    && string.IsNullOrWhiteSpace(node.TimerExpression))
                {
                    errors.Add($"Timer node '{node.Id}' requires timerExpression when timerType is 'expression'.");
                }
            }

            if (node.Type == "notification"
                && string.IsNullOrWhiteSpace(node.NotificationKey)
                && string.IsNullOrWhiteSpace(node.TemplateKey))
            {
                errors.Add($"Notification node '{node.Id}' requires notificationKey or templateKey.");
            }

            if (node.Type == "servicetask"
                && string.IsNullOrWhiteSpace(node.ServiceKey)
                && string.IsNullOrWhiteSpace(node.ExternalApiEndpointId)
                && string.IsNullOrWhiteSpace(node.TargetVariable))
            {
                errors.Add($"Service task node '{node.Id}' requires serviceKey, processKey, externalApiEndpointId, or targetVariable.");
            }

            if (node.Type == "servicetask"
                && string.Equals(node.ProcessMode, "dataUpdate", StringComparison.OrdinalIgnoreCase)
                && (string.IsNullOrWhiteSpace(node.TargetVariable) || string.IsNullOrWhiteSpace(node.ValueExpression)))
            {
                errors.Add($"Service task node '{node.Id}' in dataUpdate mode requires targetVariable and valueExpression.");
            }

            var outgoing = edges.Where(x => string.Equals(x.From, node.Id, StringComparison.OrdinalIgnoreCase)).ToList();
            if (node.Type != "end" && outgoing.Count == 0)
            {
                errors.Add($"Node '{node.Id}' must have an outgoing edge.");
            }

            if (node.Type == "condition")
            {
                var outcomes = outgoing.Where(x => !string.IsNullOrWhiteSpace(x.Outcome)).Select(x => x.Outcome!).ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (outgoing.Count != 2)
                {
                    errors.Add($"Condition node '{node.Id}' must have exactly two outgoing edges.");
                }

                if (!outcomes.SetEquals(["true", "false"]))
                {
                    errors.Add($"Condition node '{node.Id}' must have one true outcome edge and one false outcome edge.");
                }
            }
            else if (outgoing.Count > 1)
            {
                errors.Add($"Node '{node.Id}' has multiple outgoing edges.");
            }
        }
    }

    private static void ValidateGraphRules(IReadOnlyDictionary<string, WorkflowNode> nodes, IReadOnlyList<WorkflowEdge> edges, List<string> errors)
    {
        var startNode = nodes.Values.SingleOrDefault(x => x.Type == "start");
        if (startNode is null)
        {
            return;
        }

        var reachable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>();
        queue.Enqueue(startNode.Id);

        while (queue.Count > 0)
        {
            var nodeId = queue.Dequeue();
            if (!reachable.Add(nodeId))
            {
                continue;
            }

            foreach (var edge in edges.Where(x => string.Equals(x.From, nodeId, StringComparison.OrdinalIgnoreCase)))
            {
                if (nodes.ContainsKey(edge.To))
                {
                    queue.Enqueue(edge.To);
                }
            }
        }

        foreach (var node in nodes.Values.Where(x => x.Type != "start" && !reachable.Contains(x.Id)))
        {
            errors.Add($"Node '{node.Id}' is not reachable from the start node.");
        }
    }

    private static string? ReadRequiredString(JsonElement element, string propertyName, List<string> errors)
    {
        var value = ReadOptionalString(element, propertyName);
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"'{propertyName}' is required.");
            return null;
        }

        return value;
    }

    private static string? ReadOptionalString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() : property.ToString();
    }

    private static Guid? ReadOptionalGuid(JsonElement element, string propertyName)
    {
        var value = ReadOptionalString(element, propertyName);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private static int? ReadOptionalInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var value) ? value : null;
    }

    private static bool IsSupportedTimerExpression(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return DateTime.TryParse(value, out _)
            || TimeSpan.TryParse(value, out _)
            || TryParseXmlDuration(value, out _)
            || value.Contains('(');
    }

    private static bool TryParseXmlDuration(string value, out TimeSpan duration)
    {
        try
        {
            duration = XmlConvert.ToTimeSpan(value);
            return true;
        }
        catch (FormatException)
        {
            duration = default;
            return false;
        }
    }

    private static string? ReadOptionalJson(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return property.GetRawText();
    }
}

internal sealed record WorkflowNode(
    string Id,
    string Type,
    string? Name,
    Guid? AssignedToUserId,
    Guid? AssignedToGroupId,
    Guid? AssignedToRoleId,
    string? AssignmentType,
    string? AssignmentExpression,
    string? Expression,
    int? DueInHours,
    string? TimerType,
    string? TimerExpression,
    string? ProcessMode,
    string? ServiceKey,
    string? ExternalApiEndpointId,
    string? InputMapping,
    string? OutputMapping,
    string? TargetVariable,
    string? Operation,
    string? ValueExpression,
    string? RetryPolicy,
    string? NotificationKey,
    string? Channel,
    string? TemplateKey,
    string? MetadataJson);

internal sealed record WorkflowEdge(string From, string To, string? Outcome);

internal sealed record WorkflowDefinitionValidationResult(
    bool IsValid,
    WorkflowDefinitionDocument? Document,
    IReadOnlyList<string> Errors)
{
    public static WorkflowDefinitionValidationResult Valid(WorkflowDefinitionDocument document) => new(true, document, []);
    public static WorkflowDefinitionValidationResult Invalid(string error) => new(false, null, [error]);
    public static WorkflowDefinitionValidationResult Invalid(IReadOnlyList<string> errors) => new(false, null, errors);
}

internal sealed class WorkflowDefinitionException(string message) : Exception(message);
