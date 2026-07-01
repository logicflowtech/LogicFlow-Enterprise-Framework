/*
    Seeds a working vertical-slice workflow:
    Start -> Approval -> End

    The approval step is assigned to the first active user in dbo.AspNetUsers.
*/

USE [LogicFlowEnterpriseFrameworkDb];
GO

SET NOCOUNT ON;
GO

DECLARE @AssignedToUserId UNIQUEIDENTIFIER;
SELECT TOP (1) @AssignedToUserId = Id
FROM dbo.AspNetUsers
WHERE IsActive = 1
ORDER BY UserName;

IF @AssignedToUserId IS NULL
BEGIN
    RAISERROR('No active user exists in dbo.AspNetUsers. Seed at least one user before running this script.', 16, 1);
    RETURN;
END

DECLARE @WorkflowCode NVARCHAR(128) = N'SAMPLE-APPROVAL';
IF EXISTS (SELECT 1 FROM wf.WorkflowDefinitions WHERE WorkflowCode = @WorkflowCode)
BEGIN
    PRINT 'Sample workflow already exists. Skipping seed.';
    RETURN;
END

DECLARE @DefinitionId UNIQUEIDENTIFIER = NEWID();
DECLARE @DraftId UNIQUEIDENTIFIER = NEWID();
DECLARE @VersionId UNIQUEIDENTIFIER = NEWID();
DECLARE @Now DATETIME2(7) = SYSUTCDATETIME();
DECLARE @AssignedUserIdText NVARCHAR(36) = CONVERT(NVARCHAR(36), @AssignedToUserId);

DECLARE @DefinitionJson NVARCHAR(MAX) = N'{
  "schemaVersion": 2,
  "metadata": {
    "name": "Sample Approval Workflow"
  },
  "nodes": [
    { "id": "start", "type": "start" },
    { "id": "managerApproval", "type": "approval", "name": "Manager Approval", "assignedToUserId": "' + @AssignedUserIdText + N'", "assignmentType": "User", "dueInHours": 24 },
    { "id": "end", "type": "end" }
  ],
  "edges": [
    { "from": "start", "to": "managerApproval" },
    { "from": "managerApproval", "to": "end" }
  ]
}';

INSERT INTO wf.WorkflowDefinitions
(
    Id,
    WorkflowCode,
    Name,
    Description,
    Status,
    LatestVersionNumber,
    CreatedBy,
    CreatedByUserId,
    CreatedAtUtc,
    UpdatedBy,
    UpdatedByUserId,
    UpdatedAtUtc
)
VALUES
(
    @DefinitionId,
    @WorkflowCode,
    N'Sample Approval Workflow',
    N'Vertical-slice sample for Start -> Approval -> End workflow execution.',
    N'Published',
    1,
    N'system',
    @AssignedToUserId,
    @Now,
    N'system',
    @AssignedToUserId,
    @Now
);

INSERT INTO wf.WorkflowDrafts
(
    Id,
    WorkflowDefinitionId,
    DraftJson,
    SchemaVersion,
    ValidationStatus,
    CreatedBy,
    CreatedByUserId,
    CreatedAtUtc,
    UpdatedBy,
    UpdatedByUserId,
    UpdatedAtUtc,
    LastAutosavedAtUtc
)
VALUES
(
    @DraftId,
    @DefinitionId,
    @DefinitionJson,
    2,
    N'Valid',
    N'system',
    @AssignedToUserId,
    @Now,
    N'system',
    @AssignedToUserId,
    @Now,
    @Now
);

UPDATE wf.WorkflowDefinitions
SET CurrentDraftId = @DraftId
WHERE Id = @DefinitionId;

INSERT INTO wf.WorkflowVersions
(
    Id,
    WorkflowDefinitionId,
    VersionNumber,
    DefinitionJson,
    Status,
    EffectiveFromUtc,
    PublishedBy,
    PublishedByUserId,
    PublishedAtUtc,
    PublishMessage,
    CreatedBy,
    CreatedAtUtc
)
VALUES
(
    @VersionId,
    @DefinitionId,
    1,
    @DefinitionJson,
    N'Published',
    @Now,
    N'system',
    @AssignedToUserId,
    @Now,
    N'Initial seeded sample workflow.',
    N'system',
    @Now
);

SELECT
    @DefinitionId AS WorkflowDefinitionId,
    @VersionId AS WorkflowVersionId,
    @AssignedToUserId AS AssignedToUserId,
    @WorkflowCode AS WorkflowCode;
GO
