using LogicFlowEnterpriseFramework.Api.Security;
using LogicFlowEnterpriseFramework.Api.Workflow.Contracts;
using LogicFlowEnterpriseFramework.Api.Workflow.Runtime;
using LogicFlowEnterpriseFramework.Domain.Entities.Workflow;
using LogicFlowEnterpriseFramework.Infrastructure.Persistence;
using LogicFlowEnterpriseFramework.Shared.Constants;
using LogicFlowEnterpriseFramework.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogicFlowEnterpriseFramework.Api.Controllers.Workflow;

[ApiController]
[Route("api/workflow-definitions")]
public sealed class WorkflowDefinitionsController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WorkflowDefinitionResponse>>>> GetAll(CancellationToken cancellationToken)
    {
        var definitions = await (
            from definition in dbContext.WorkflowDefinitions.AsNoTracking()
            join draft in dbContext.WorkflowDrafts.AsNoTracking() on definition.CurrentDraftId equals draft.Id into draftGroup
            from draft in draftGroup.DefaultIfEmpty()
            orderby definition.Name
            select new
            {
                Definition = definition,
                Draft = draft
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<WorkflowDefinitionResponse>>.Success(
            definitions.Select(item => ToWorkflowDefinitionResponse(item.Definition, item.Draft)).ToList()));
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<WorkflowDefinitionResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var definition = await GetDefinitionResponseAsync(id, cancellationToken);
        return definition is null
            ? NotFound(ApiResponse<WorkflowDefinitionResponse>.Failure("Workflow definition not found."))
            : Ok(ApiResponse<WorkflowDefinitionResponse>.Success(definition));
    }

    [HttpGet("{id:guid}/versions")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WorkflowVersionResponse>>>> GetVersions(Guid id, CancellationToken cancellationToken)
    {
        if (!await dbContext.WorkflowDefinitions.AsNoTracking().AnyAsync(x => x.Id == id, cancellationToken))
        {
            return NotFound(ApiResponse<IReadOnlyList<WorkflowVersionResponse>>.Failure("Workflow definition not found."));
        }

        var versions = await dbContext.WorkflowVersions.AsNoTracking()
            .Where(x => x.WorkflowDefinitionId == id)
            .OrderByDescending(x => x.VersionNumber)
            .Select(x => new WorkflowVersionResponse(x.Id, x.WorkflowDefinitionId, x.VersionNumber, x.Status, x.EffectiveFromUtc, x.EffectiveToUtc, x.PublishedBy, x.PublishedAtUtc, x.PublishMessage))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<WorkflowVersionResponse>>.Success(versions));
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ApiResponse<WorkflowDefinitionResponse>>> Create(CreateWorkflowDefinitionRequest request, CancellationToken cancellationToken)
    {
        var actor = User.GetWorkflowActor();
        var now = DateTime.UtcNow;
        var definitionId = Guid.NewGuid();
        var draftId = Guid.NewGuid();
        var workflowName = string.IsNullOrWhiteSpace(request.Name) ? "New Workflow" : request.Name.Trim();

        var definition = new WorkflowDefinition
        {
            Id = definitionId,
            WorkflowCode = BuildWorkflowCode(workflowName, definitionId),
            Name = workflowName,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Status = "Draft",
            CreatedBy = actor.UserName,
            CreatedByUserId = actor.UserId,
            CreatedAtUtc = now
        };

        var draft = new WorkflowDraft
        {
            Id = draftId,
            WorkflowDefinitionId = definitionId,
            DraftJson = string.IsNullOrWhiteSpace(request.DraftDefinitionJson) ? "{}" : request.DraftDefinitionJson,
            SchemaVersion = 2,
            ValidationStatus = "Pending",
            CreatedBy = actor.UserName,
            CreatedByUserId = actor.UserId,
            CreatedAtUtc = now,
            UpdatedBy = actor.UserName,
            UpdatedByUserId = actor.UserId,
            UpdatedAtUtc = now
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        dbContext.WorkflowDefinitions.Add(definition);
        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.WorkflowDrafts.Add(draft);
        await dbContext.SaveChangesAsync(cancellationToken);

        definition.CurrentDraftId = draftId;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var response = await GetDefinitionResponseAsync(definition.Id, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = definition.Id }, ApiResponse<WorkflowDefinitionResponse>.Success(response!, "Workflow definition created."));
    }

    [HttpPost("validate")]
    [Authorize]
    public ActionResult<ApiResponse<WorkflowValidationResponse>> Validate(ValidateWorkflowDefinitionRequest request)
    {
        var validation = WorkflowDefinitionDocument.Validate(request.DefinitionJson);
        return Ok(ApiResponse<WorkflowValidationResponse>.Success(new WorkflowValidationResponse(validation.IsValid, validation.Errors)));
    }

    [HttpPost("{id:guid}/validate")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<WorkflowValidationResponse>>> ValidateDraft(Guid id, CancellationToken cancellationToken)
    {
        var definition = await dbContext.WorkflowDefinitions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (definition is null || !definition.CurrentDraftId.HasValue)
        {
            return NotFound(ApiResponse<WorkflowValidationResponse>.Failure("Workflow definition not found."));
        }

        var draft = await dbContext.WorkflowDrafts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == definition.CurrentDraftId.Value, cancellationToken);
        if (draft is null)
        {
            return NotFound(ApiResponse<WorkflowValidationResponse>.Failure("Workflow draft not found."));
        }

        var validation = WorkflowDefinitionDocument.Validate(draft.DraftJson);
        return Ok(ApiResponse<WorkflowValidationResponse>.Success(new WorkflowValidationResponse(validation.IsValid, validation.Errors)));
    }

    [HttpPut("{id:guid}/draft")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<WorkflowDefinitionResponse>>> UpdateDraft(Guid id, UpdateWorkflowDraftRequest request, CancellationToken cancellationToken)
    {
        var actor = User.GetWorkflowActor();
        var definition = await dbContext.WorkflowDefinitions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (definition is null)
        {
            return NotFound(ApiResponse<WorkflowDefinitionResponse>.Failure("Workflow definition not found."));
        }

        if (!definition.CurrentDraftId.HasValue)
        {
            return BadRequest(ApiResponse<WorkflowDefinitionResponse>.Failure("Workflow draft not found."));
        }

        if (!string.Equals(definition.Status, "Draft", StringComparison.OrdinalIgnoreCase) && !string.Equals(definition.Status, "Published", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(ApiResponse<WorkflowDefinitionResponse>.Failure("Only draft or published workflow definitions can be edited."));
        }

        var draft = await dbContext.WorkflowDrafts.FirstOrDefaultAsync(x => x.Id == definition.CurrentDraftId.Value, cancellationToken);
        if (draft is null)
        {
            return NotFound(ApiResponse<WorkflowDefinitionResponse>.Failure("Workflow draft not found."));
        }

        if (!TryApplyConcurrencyToken(dbContext.Entry(definition).Property(x => x.RowVersion), request.DefinitionRowVersion))
        {
            return BadRequest(ApiResponse<WorkflowDefinitionResponse>.Failure("Definition row version is required."));
        }

        if (!TryApplyConcurrencyToken(dbContext.Entry(draft).Property(x => x.RowVersion), request.DraftRowVersion))
        {
            return BadRequest(ApiResponse<WorkflowDefinitionResponse>.Failure("Draft row version is required."));
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            definition.Name = request.Name.Trim();
        }

        definition.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        definition.Status = "Draft";
        definition.UpdatedBy = actor.UserName;
        definition.UpdatedByUserId = actor.UserId;
        definition.UpdatedAtUtc = DateTime.UtcNow;
        draft.DraftJson = request.DraftDefinitionJson;
        draft.ValidationStatus = "Pending";
        draft.ValidationErrorsJson = null;
        draft.LastAutosavedAtUtc = DateTime.UtcNow;
        draft.UpdatedBy = actor.UserName;
        draft.UpdatedByUserId = actor.UserId;
        draft.UpdatedAtUtc = DateTime.UtcNow;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(ApiResponse<WorkflowDefinitionResponse>.Failure("This workflow draft was changed by another session. Refresh the definition and try again."));
        }

        var response = await GetDefinitionResponseAsync(definition.Id, cancellationToken);
        return Ok(ApiResponse<WorkflowDefinitionResponse>.Success(response!, "Draft updated."));
    }

    [HttpPost("{id:guid}/publish")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<WorkflowVersionResponse>>> Publish(Guid id, PublishWorkflowDefinitionRequest request, CancellationToken cancellationToken)
    {
        var actor = User.GetWorkflowActor();
        var definition = await dbContext.WorkflowDefinitions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (definition is null)
        {
            return NotFound(ApiResponse<WorkflowVersionResponse>.Failure("Workflow definition not found."));
        }

        if (!definition.CurrentDraftId.HasValue)
        {
            return BadRequest(ApiResponse<WorkflowVersionResponse>.Failure("Workflow draft is required before publishing."));
        }

        var draft = await dbContext.WorkflowDrafts.FirstOrDefaultAsync(x => x.Id == definition.CurrentDraftId.Value, cancellationToken);
        if (draft is null || string.IsNullOrWhiteSpace(draft.DraftJson))
        {
            return BadRequest(ApiResponse<WorkflowVersionResponse>.Failure("Draft JSON is required before publishing."));
        }

        if (!TryApplyConcurrencyToken(dbContext.Entry(definition).Property(x => x.RowVersion), request.DefinitionRowVersion))
        {
            return BadRequest(ApiResponse<WorkflowVersionResponse>.Failure("Definition row version is required."));
        }

        if (!TryApplyConcurrencyToken(dbContext.Entry(draft).Property(x => x.RowVersion), request.DraftRowVersion))
        {
            return BadRequest(ApiResponse<WorkflowVersionResponse>.Failure("Draft row version is required."));
        }

        var validation = WorkflowDefinitionDocument.Validate(draft.DraftJson);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<WorkflowVersionResponse>.Failure(string.Join(" ", validation.Errors)));
        }

        var nextVersionNumber = await dbContext.WorkflowVersions.Where(x => x.WorkflowDefinitionId == id).Select(x => (int?)x.VersionNumber).MaxAsync(cancellationToken) ?? 0;
        var now = DateTime.UtcNow;
        var version = new WorkflowVersion
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = definition.Id,
            VersionNumber = nextVersionNumber + 1,
            DefinitionJson = draft.DraftJson,
            Status = "Published",
            EffectiveFromUtc = request.EffectiveFromUtc ?? now,
            EffectiveToUtc = request.EffectiveToUtc,
            PublishedBy = actor.UserName,
            PublishedByUserId = actor.UserId,
            PublishedAtUtc = now,
            PublishMessage = string.IsNullOrWhiteSpace(request.PublishMessage) ? null : request.PublishMessage.Trim(),
            CreatedBy = actor.UserName,
            CreatedAtUtc = now
        };

        definition.Status = "Published";
        definition.LatestVersionNumber = version.VersionNumber;
        definition.UpdatedBy = actor.UserName;
        definition.UpdatedByUserId = actor.UserId;
        definition.UpdatedAtUtc = now;
        draft.ValidationStatus = "Valid";
        draft.ValidationErrorsJson = null;
        draft.UpdatedBy = actor.UserName;
        draft.UpdatedByUserId = actor.UserId;
        draft.UpdatedAtUtc = now;

        dbContext.WorkflowVersions.Add(version);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(ApiResponse<WorkflowVersionResponse>.Failure("This workflow draft was changed by another session. Refresh the definition and try again."));
        }

        return Ok(ApiResponse<WorkflowVersionResponse>.Success(
            new WorkflowVersionResponse(version.Id, version.WorkflowDefinitionId, version.VersionNumber, version.Status, version.EffectiveFromUtc, version.EffectiveToUtc, version.PublishedBy, version.PublishedAtUtc, version.PublishMessage),
            "Workflow version published."));
    }

    private async Task<WorkflowDefinitionResponse?> GetDefinitionResponseAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await (
            from definition in dbContext.WorkflowDefinitions.AsNoTracking()
            join draft in dbContext.WorkflowDrafts.AsNoTracking() on definition.CurrentDraftId equals draft.Id into draftGroup
            from draft in draftGroup.DefaultIfEmpty()
            where definition.Id == id
            select new
            {
                Definition = definition,
                Draft = draft
            })
            .FirstOrDefaultAsync(cancellationToken);

        return result is null ? null : ToWorkflowDefinitionResponse(result.Definition, result.Draft);
    }

    private static string BuildWorkflowCode(string name, Guid definitionId)
    {
        var seed = new string((name ?? "workflow").Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).Take(12).ToArray());
        if (string.IsNullOrWhiteSpace(seed))
        {
            seed = "WORKFLOW";
        }

        return $"{seed}-{definitionId.ToString("N")[..8]}";
    }

    private static WorkflowDefinitionResponse ToWorkflowDefinitionResponse(WorkflowDefinition definition, WorkflowDraft? draft)
    {
        return new WorkflowDefinitionResponse(
            definition.Id,
            definition.Name,
            definition.Description,
            definition.Status,
            draft?.DraftJson,
            EncodeRowVersion(definition.RowVersion),
            draft is null ? null : EncodeRowVersion(draft.RowVersion),
            definition.CreatedAtUtc,
            definition.UpdatedAtUtc);
    }

    private static string EncodeRowVersion(byte[] rowVersion) => Convert.ToBase64String(rowVersion);

    private static bool TryApplyConcurrencyToken(Microsoft.EntityFrameworkCore.ChangeTracking.PropertyEntry property, string? encodedRowVersion)
    {
        if (string.IsNullOrWhiteSpace(encodedRowVersion))
        {
            return false;
        }

        try
        {
            property.OriginalValue = Convert.FromBase64String(encodedRowVersion);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
