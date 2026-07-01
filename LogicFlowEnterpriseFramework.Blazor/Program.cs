using LogicFlowEnterpriseFramework.Blazor.Components;
using LogicFlowEnterpriseFramework.Blazor.Features.Administration;
using LogicFlowEnterpriseFramework.Blazor.Features.Identity;
using LogicFlowEnterpriseFramework.Blazor.Features.Workflow;
using LogicFlowEnterpriseFramework.Blazor.Features.Workspace;
using LogicFlowEnterpriseFramework.Blazor.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddDataProtection()
    .SetApplicationName("LogicFlowEnterpriseFramework.Blazor")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, ".keys")));
builder.Services.AddHttpClient<LogicFlowApiClient>(client =>
{
    var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5116";
    client.BaseAddress = new Uri(apiBaseUrl);
});
builder.Services.AddHttpClient<WorkflowOperationsClient>(client =>
{
    var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5116";
    client.BaseAddress = new Uri(apiBaseUrl);
});
builder.Services.AddHttpClient("WorkflowApiProxy", client =>
{
    var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5116";
    client.BaseAddress = new Uri(apiBaseUrl);
});
builder.Services.AddScoped<AuthSession>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddPlatformFeatures(features =>
{
    features.AddFeature<IdentityFeature>();
    features.AddFeature<WorkspaceFeature>();
    features.AddFeature<WorkflowFeature>();
    features.AddFeature<AdministrationFeature>();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapMethods("/workflow-api/{**path}", ["GET", "POST", "PUT", "DELETE", "PATCH"], async (
    HttpContext httpContext,
    IHttpClientFactory httpClientFactory,
    ILogger<Program> logger,
    string? path) =>
{
    var client = httpClientFactory.CreateClient("WorkflowApiProxy");
    var targetPath = string.IsNullOrWhiteSpace(path) ? string.Empty : path;
    var targetUri = $"{targetPath}{httpContext.Request.QueryString}";

    using var proxyRequest = new HttpRequestMessage(new HttpMethod(httpContext.Request.Method), targetUri);

    if (httpContext.Request.ContentLength > 0 || httpContext.Request.Headers.ContainsKey("Transfer-Encoding"))
    {
        proxyRequest.Content = new StreamContent(httpContext.Request.Body);
        if (!string.IsNullOrWhiteSpace(httpContext.Request.ContentType))
        {
            proxyRequest.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(httpContext.Request.ContentType);
        }
    }

    foreach (var header in httpContext.Request.Headers)
    {
        if (string.Equals(header.Key, "Host", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(header.Key, "Content-Length", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(header.Key, "Transfer-Encoding", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(header.Key, "Connection", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (!proxyRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
        {
            proxyRequest.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }
    }

    try
    {
        using var responseMessage = await client.SendAsync(proxyRequest, HttpCompletionOption.ResponseHeadersRead, httpContext.RequestAborted);
        httpContext.Response.StatusCode = (int)responseMessage.StatusCode;

        foreach (var header in responseMessage.Headers)
        {
            httpContext.Response.Headers[header.Key] = header.Value.ToArray();
        }

        foreach (var header in responseMessage.Content.Headers)
        {
            httpContext.Response.Headers[header.Key] = header.Value.ToArray();
        }

        httpContext.Response.Headers.Remove("transfer-encoding");
        await responseMessage.Content.CopyToAsync(httpContext.Response.Body);
    }
    catch (HttpRequestException ex)
    {
        logger.LogError(ex, "Workflow API proxy failed for {Method} {TargetUri}.", httpContext.Request.Method, targetUri);

        if (!httpContext.Response.HasStarted)
        {
            httpContext.Response.StatusCode = StatusCodes.Status502BadGateway;
            httpContext.Response.ContentType = "application/json";

            var payload = JsonSerializer.Serialize(new
            {
                succeeded = false,
                message = "Workflow API is unavailable. Verify the API service is running and the configured Api:BaseUrl is correct."
            });

            await httpContext.Response.WriteAsync(payload);
        }
    }
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
