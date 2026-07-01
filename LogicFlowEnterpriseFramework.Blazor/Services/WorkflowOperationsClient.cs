using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LogicFlowEnterpriseFramework.Blazor.Models;

namespace LogicFlowEnterpriseFramework.Blazor.Services;

public sealed class WorkflowOperationsClient(HttpClient httpClient, AuthSession authSession)
{
    public async Task<ApiResponse<List<WorkflowDefinitionSummaryModel>>> GetDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, "/api/workflow-definitions");
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<List<WorkflowDefinitionSummaryModel>>(response, cancellationToken);
    }

    public async Task<ApiResponse<WorkflowDefinitionSummaryModel>> CreateDefinitionAsync(CreateWorkflowDefinitionRequestModel requestModel, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, "/api/workflow-definitions");
        request.Content = JsonContent.Create(requestModel);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<WorkflowDefinitionSummaryModel>(response, cancellationToken);
    }

    public async Task<ApiResponse<List<WorkflowVersionDetailModel>>> GetDefinitionVersionsAsync(Guid definitionId, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, $"/api/workflow-definitions/{definitionId}/versions");
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<List<WorkflowVersionDetailModel>>(response, cancellationToken);
    }

    public async Task<ApiResponse<WorkflowVersionDetailModel>> PublishDefinitionAsync(Guid definitionId, PublishWorkflowDefinitionRequestModel requestModel, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, $"/api/workflow-definitions/{definitionId}/publish");
        request.Content = JsonContent.Create(requestModel);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<WorkflowVersionDetailModel>(response, cancellationToken);
    }

    public async Task<ApiResponse<WorkflowInstanceModel>> StartWorkflowAsync(Guid workflowDefinitionId, StartWorkflowActionRequestModel requestModel, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, $"/api/workflows/{workflowDefinitionId}/start");
        request.Content = JsonContent.Create(requestModel);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<WorkflowInstanceModel>(response, cancellationToken);
    }

    public async Task<ApiResponse<List<WorkflowTaskModel>>> GetMyTasksAsync(CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, "/api/tasks/my");
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<List<WorkflowTaskModel>>(response, cancellationToken);
    }

    public async Task<ApiResponse<WorkflowTaskModel>> ClaimTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, $"/api/tasks/{taskId}/claim");
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<WorkflowTaskModel>(response, cancellationToken);
    }

    public async Task<ApiResponse<WorkflowTaskModel>> UnclaimTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, $"/api/tasks/{taskId}/unclaim");
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<WorkflowTaskModel>(response, cancellationToken);
    }

    public async Task<ApiResponse<WorkflowTaskModel>> ApproveTaskAsync(Guid taskId, CompleteTaskActionRequestModel requestModel, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, $"/api/tasks/{taskId}/approve");
        request.Content = JsonContent.Create(requestModel);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<WorkflowTaskModel>(response, cancellationToken);
    }

    public async Task<ApiResponse<WorkflowTaskModel>> RejectTaskAsync(Guid taskId, CompleteTaskActionRequestModel requestModel, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, $"/api/tasks/{taskId}/reject");
        request.Content = JsonContent.Create(requestModel);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<WorkflowTaskModel>(response, cancellationToken);
    }

    public async Task<ApiResponse<WorkflowTaskModel>> DelegateTaskAsync(Guid taskId, DelegateTaskActionRequestModel requestModel, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, $"/api/tasks/{taskId}/delegate");
        request.Content = JsonContent.Create(requestModel);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<WorkflowTaskModel>(response, cancellationToken);
    }

    public async Task<ApiResponse<WorkflowTaskModel>> ReassignTaskAsync(Guid taskId, ReassignTaskActionRequestModel requestModel, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, $"/api/tasks/{taskId}/reassign");
        request.Content = JsonContent.Create(requestModel);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<WorkflowTaskModel>(response, cancellationToken);
    }

    public async Task<ApiResponse<WorkflowTaskCommentModel>> AddTaskCommentAsync(Guid taskId, AddTaskCommentActionRequestModel requestModel, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, $"/api/tasks/{taskId}/comment");
        request.Content = JsonContent.Create(requestModel);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<WorkflowTaskCommentModel>(response, cancellationToken);
    }

    public async Task<ApiResponse<List<WorkflowUserLookupModel>>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, "/api/workflow/lookups/users");
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<List<WorkflowUserLookupModel>>(response, cancellationToken);
    }

    public async Task<ApiResponse<List<WorkflowInstanceListItemModel>>> GetInstancesAsync(Guid? workflowDefinitionId = null, string? status = null, string? search = null, int take = 100, CancellationToken cancellationToken = default)
    {
        var parameters = new List<string> { $"take={Math.Clamp(take, 1, 200)}" };

        if (workflowDefinitionId.HasValue)
        {
            parameters.Add($"workflowDefinitionId={workflowDefinitionId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            parameters.Add($"status={Uri.EscapeDataString(status)}");
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            parameters.Add($"search={Uri.EscapeDataString(search)}");
        }

        var query = string.Join("&", parameters);
        using var request = CreateRequest(HttpMethod.Get, $"/api/workflow-instances?{query}");
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<List<WorkflowInstanceListItemModel>>(response, cancellationToken);
    }

    public async Task<ApiResponse<WorkflowInstanceDetailModel>> GetInstanceDetailAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, $"/api/workflow-instances/{instanceId}/detail");
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<WorkflowInstanceDetailModel>(response, cancellationToken);
    }

    public async Task<ApiResponse<WorkflowInstanceModel>> CancelInstanceAsync(Guid instanceId, CancelWorkflowInstanceActionRequestModel requestModel, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, $"/api/workflow-instances/{instanceId}/cancel");
        request.Content = JsonContent.Create(requestModel);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<WorkflowInstanceModel>(response, cancellationToken);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string uri)
    {
        var request = new HttpRequestMessage(method, uri);

        if (!string.IsNullOrWhiteSpace(authSession.AccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authSession.AccessToken);
        }

        return request;
    }

    private static async Task<ApiResponse<T>> ReadResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = response.Content is null
            ? string.Empty
            : await response.Content.ReadAsStringAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(content))
        {
            return new ApiResponse<T>
            {
                Succeeded = false,
                Message = $"API returned HTTP {(int)response.StatusCode} with an empty response body."
            };
        }

        try
        {
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<T>>(content);
            if (apiResponse is not null)
            {
                return apiResponse;
            }
        }
        catch (JsonException)
        {
            var preview = content.Length > 220 ? $"{content[..220]}..." : content;

            return new ApiResponse<T>
            {
                Succeeded = false,
                Message = $"API returned HTTP {(int)response.StatusCode} with a non-JSON response: {preview}"
            };
        }

        return new ApiResponse<T>
        {
            Succeeded = false,
            Message = $"API returned HTTP {(int)response.StatusCode}."
        };
    }
}
