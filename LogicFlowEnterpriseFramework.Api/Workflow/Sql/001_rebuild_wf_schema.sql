/*
    Enterprise workflow schema rebuild for LogicFlowEnterpriseFrameworkDb.

    Purpose:
    - remove the existing wf.* workflow tables
    - create a richer SQL Server schema for standalone designer + .NET runtime

    Important:
    - this is destructive for the existing wf workflow data
    - apply together with the backend/API migration that reads the new table model
*/

USE [LogicFlowEnterpriseFrameworkDb];
GO

SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

IF SCHEMA_ID(N'wf') IS NULL
BEGIN
    EXEC(N'CREATE SCHEMA [wf]');
END
GO

/* Drop old foreign keys first */
IF OBJECT_ID(N'wf.FK_WorkflowAuditLogs_AspNetUsers_PerformedByUserId', N'F') IS NOT NULL
    ALTER TABLE wf.WorkflowAuditLogs DROP CONSTRAINT FK_WorkflowAuditLogs_AspNetUsers_PerformedByUserId;
GO
IF OBJECT_ID(N'wf.FK_WorkflowAuditLogs_WorkflowInstances_WorkflowInstanceId', N'F') IS NOT NULL
    ALTER TABLE wf.WorkflowAuditLogs DROP CONSTRAINT FK_WorkflowAuditLogs_WorkflowInstances_WorkflowInstanceId;
GO
IF OBJECT_ID(N'wf.FK_WorkflowAuditLogs_WorkflowTasks_WorkflowTaskId', N'F') IS NOT NULL
    ALTER TABLE wf.WorkflowAuditLogs DROP CONSTRAINT FK_WorkflowAuditLogs_WorkflowTasks_WorkflowTaskId;
GO
IF OBJECT_ID(N'wf.FK_WorkflowInstances_AspNetUsers_StartedByUserId', N'F') IS NOT NULL
    ALTER TABLE wf.WorkflowInstances DROP CONSTRAINT FK_WorkflowInstances_AspNetUsers_StartedByUserId;
GO
IF OBJECT_ID(N'wf.FK_WorkflowInstances_WorkflowDefinitions_WorkflowDefinitionId', N'F') IS NOT NULL
    ALTER TABLE wf.WorkflowInstances DROP CONSTRAINT FK_WorkflowInstances_WorkflowDefinitions_WorkflowDefinitionId;
GO
IF OBJECT_ID(N'wf.FK_WorkflowInstances_WorkflowVersions_WorkflowVersionId', N'F') IS NOT NULL
    ALTER TABLE wf.WorkflowInstances DROP CONSTRAINT FK_WorkflowInstances_WorkflowVersions_WorkflowVersionId;
GO
IF OBJECT_ID(N'wf.FK_WorkflowTasks_AspNetUsers_AssignedToUserId', N'F') IS NOT NULL
    ALTER TABLE wf.WorkflowTasks DROP CONSTRAINT FK_WorkflowTasks_AspNetUsers_AssignedToUserId;
GO
IF OBJECT_ID(N'wf.FK_WorkflowTasks_AspNetUsers_ClaimedByUserId', N'F') IS NOT NULL
    ALTER TABLE wf.WorkflowTasks DROP CONSTRAINT FK_WorkflowTasks_AspNetUsers_ClaimedByUserId;
GO
IF OBJECT_ID(N'wf.FK_WorkflowTasks_AspNetUsers_CompletedByUserId', N'F') IS NOT NULL
    ALTER TABLE wf.WorkflowTasks DROP CONSTRAINT FK_WorkflowTasks_AspNetUsers_CompletedByUserId;
GO
IF OBJECT_ID(N'wf.FK_WorkflowTasks_PlatformAccessGroups_AssignedToGroupId', N'F') IS NOT NULL
    ALTER TABLE wf.WorkflowTasks DROP CONSTRAINT FK_WorkflowTasks_PlatformAccessGroups_AssignedToGroupId;
GO
IF OBJECT_ID(N'wf.FK_WorkflowTasks_PlatformAccessRoles_AssignedToRoleId', N'F') IS NOT NULL
    ALTER TABLE wf.WorkflowTasks DROP CONSTRAINT FK_WorkflowTasks_PlatformAccessRoles_AssignedToRoleId;
GO
IF OBJECT_ID(N'wf.FK_WorkflowTasks_WorkflowInstances_WorkflowInstanceId', N'F') IS NOT NULL
    ALTER TABLE wf.WorkflowTasks DROP CONSTRAINT FK_WorkflowTasks_WorkflowInstances_WorkflowInstanceId;
GO
IF OBJECT_ID(N'wf.FK_WorkflowVariables_WorkflowInstances_WorkflowInstanceId', N'F') IS NOT NULL
    ALTER TABLE wf.WorkflowVariables DROP CONSTRAINT FK_WorkflowVariables_WorkflowInstances_WorkflowInstanceId;
GO
IF OBJECT_ID(N'wf.FK_WorkflowVersions_WorkflowDefinitions_WorkflowDefinitionId', N'F') IS NOT NULL
    ALTER TABLE wf.WorkflowVersions DROP CONSTRAINT FK_WorkflowVersions_WorkflowDefinitions_WorkflowDefinitionId;
GO

/* Drop old workflow tables */
IF OBJECT_ID(N'wf.WorkflowAuditLogs', N'U') IS NOT NULL DROP TABLE wf.WorkflowAuditLogs;
GO
IF OBJECT_ID(N'wf.WorkflowVariables', N'U') IS NOT NULL DROP TABLE wf.WorkflowVariables;
GO
IF OBJECT_ID(N'wf.WorkflowTasks', N'U') IS NOT NULL DROP TABLE wf.WorkflowTasks;
GO
IF OBJECT_ID(N'wf.WorkflowInstances', N'U') IS NOT NULL DROP TABLE wf.WorkflowInstances;
GO
IF OBJECT_ID(N'wf.WorkflowVersions', N'U') IS NOT NULL DROP TABLE wf.WorkflowVersions;
GO
IF OBJECT_ID(N'wf.WorkflowDefinitions', N'U') IS NOT NULL DROP TABLE wf.WorkflowDefinitions;
GO

/* Design-time */
CREATE TABLE wf.WorkflowDefinitions
(
    Id UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT PK_wf_WorkflowDefinitions PRIMARY KEY
        CONSTRAINT DF_wf_WorkflowDefinitions_Id DEFAULT NEWSEQUENTIALID(),
    TenantId UNIQUEIDENTIFIER NULL,
    WorkflowCode NVARCHAR(128) NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(1000) NULL,
    Status NVARCHAR(40) NOT NULL
        CONSTRAINT DF_wf_WorkflowDefinitions_Status DEFAULT N'Draft',
    LatestVersionNumber INT NOT NULL
        CONSTRAINT DF_wf_WorkflowDefinitions_LatestVersionNumber DEFAULT 0,
    CurrentDraftId UNIQUEIDENTIFIER NULL,
    CreatedBy NVARCHAR(200) NOT NULL,
    CreatedByUserId UNIQUEIDENTIFIER NULL,
    CreatedAtUtc DATETIME2(7) NOT NULL
        CONSTRAINT DF_wf_WorkflowDefinitions_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    UpdatedBy NVARCHAR(200) NULL,
    UpdatedByUserId UNIQUEIDENTIFIER NULL,
    UpdatedAtUtc DATETIME2(7) NULL,
    RowVersion ROWVERSION NOT NULL,

    CONSTRAINT UQ_wf_WorkflowDefinitions_WorkflowCode UNIQUE (WorkflowCode),
    CONSTRAINT CK_wf_WorkflowDefinitions_Status CHECK (Status IN (N'Draft', N'Published', N'Archived'))
);
GO

CREATE TABLE wf.WorkflowDrafts
(
    Id UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT PK_wf_WorkflowDrafts PRIMARY KEY
        CONSTRAINT DF_wf_WorkflowDrafts_Id DEFAULT NEWSEQUENTIALID(),
    WorkflowDefinitionId UNIQUEIDENTIFIER NOT NULL,
    DraftJson NVARCHAR(MAX) NOT NULL,
    SchemaVersion INT NOT NULL
        CONSTRAINT DF_wf_WorkflowDrafts_SchemaVersion DEFAULT 1,
    ValidationStatus NVARCHAR(30) NOT NULL
        CONSTRAINT DF_wf_WorkflowDrafts_ValidationStatus DEFAULT N'Pending',
    ValidationErrorsJson NVARCHAR(MAX) NULL,
    DesignerMetadataJson NVARCHAR(MAX) NULL,
    LockedBy NVARCHAR(200) NULL,
    LockedByUserId UNIQUEIDENTIFIER NULL,
    LockedAtUtc DATETIME2(7) NULL,
    LastAutosavedAtUtc DATETIME2(7) NULL,
    CreatedBy NVARCHAR(200) NOT NULL,
    CreatedByUserId UNIQUEIDENTIFIER NULL,
    CreatedAtUtc DATETIME2(7) NOT NULL
        CONSTRAINT DF_wf_WorkflowDrafts_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    UpdatedBy NVARCHAR(200) NULL,
    UpdatedByUserId UNIQUEIDENTIFIER NULL,
    UpdatedAtUtc DATETIME2(7) NULL,
    RowVersion ROWVERSION NOT NULL,

    CONSTRAINT FK_wf_WorkflowDrafts_WorkflowDefinitions FOREIGN KEY (WorkflowDefinitionId) REFERENCES wf.WorkflowDefinitions(Id),
    CONSTRAINT UQ_wf_WorkflowDrafts_WorkflowDefinitionId UNIQUE (WorkflowDefinitionId),
    CONSTRAINT CK_wf_WorkflowDrafts_ValidationStatus CHECK (ValidationStatus IN (N'Pending', N'Valid', N'Invalid'))
);
GO

CREATE TABLE wf.WorkflowVersions
(
    Id UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT PK_wf_WorkflowVersions PRIMARY KEY
        CONSTRAINT DF_wf_WorkflowVersions_Id DEFAULT NEWSEQUENTIALID(),
    WorkflowDefinitionId UNIQUEIDENTIFIER NOT NULL,
    VersionNumber INT NOT NULL,
    DefinitionJson NVARCHAR(MAX) NOT NULL,
    CompiledDefinitionJson NVARCHAR(MAX) NULL,
    Status NVARCHAR(30) NOT NULL
        CONSTRAINT DF_wf_WorkflowVersions_Status DEFAULT N'Published',
    EffectiveFromUtc DATETIME2(7) NULL,
    EffectiveToUtc DATETIME2(7) NULL,
    PublishMessage NVARCHAR(2000) NULL,
    PublishedBy NVARCHAR(200) NULL,
    PublishedByUserId UNIQUEIDENTIFIER NULL,
    PublishedAtUtc DATETIME2(7) NULL,
    CreatedBy NVARCHAR(200) NOT NULL,
    CreatedAtUtc DATETIME2(7) NOT NULL
        CONSTRAINT DF_wf_WorkflowVersions_CreatedAtUtc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_wf_WorkflowVersions_WorkflowDefinitions FOREIGN KEY (WorkflowDefinitionId) REFERENCES wf.WorkflowDefinitions(Id),
    CONSTRAINT UQ_wf_WorkflowVersions_DefinitionVersion UNIQUE (WorkflowDefinitionId, VersionNumber),
    CONSTRAINT CK_wf_WorkflowVersions_Status CHECK (Status IN (N'Published', N'Retired')),
    CONSTRAINT CK_wf_WorkflowVersions_EffectiveRange CHECK (EffectiveToUtc IS NULL OR EffectiveFromUtc IS NULL OR EffectiveToUtc > EffectiveFromUtc)
);
GO

ALTER TABLE wf.WorkflowDefinitions
ADD CONSTRAINT FK_wf_WorkflowDefinitions_CurrentDraft
FOREIGN KEY (CurrentDraftId) REFERENCES wf.WorkflowDrafts(Id);
GO

/* Runtime */
CREATE TABLE wf.WorkflowInstances
(
    Id UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT PK_wf_WorkflowInstances PRIMARY KEY
        CONSTRAINT DF_wf_WorkflowInstances_Id DEFAULT NEWSEQUENTIALID(),
    TenantId UNIQUEIDENTIFIER NULL,
    WorkflowDefinitionId UNIQUEIDENTIFIER NOT NULL,
    WorkflowVersionId UNIQUEIDENTIFIER NOT NULL,
    ParentWorkflowInstanceId UNIQUEIDENTIFIER NULL,
    RootWorkflowInstanceId UNIQUEIDENTIFIER NULL,
    BusinessKey NVARCHAR(200) NULL,
    CorrelationId NVARCHAR(200) NULL,
    Title NVARCHAR(300) NULL,
    Status NVARCHAR(40) NOT NULL
        CONSTRAINT DF_wf_WorkflowInstances_Status DEFAULT N'Running',
    CurrentNodeCount INT NOT NULL
        CONSTRAINT DF_wf_WorkflowInstances_CurrentNodeCount DEFAULT 0,
    StartedBy NVARCHAR(200) NOT NULL,
    StartedByUserId UNIQUEIDENTIFIER NULL,
    StartedByDisplayName NVARCHAR(400) NULL,
    StartedAtUtc DATETIME2(7) NOT NULL
        CONSTRAINT DF_wf_WorkflowInstances_StartedAtUtc DEFAULT SYSUTCDATETIME(),
    LastHeartbeatUtc DATETIME2(7) NULL,
    CompletedAtUtc DATETIME2(7) NULL,
    CancelledAtUtc DATETIME2(7) NULL,
    FailedAtUtc DATETIME2(7) NULL,
    FailureCode NVARCHAR(100) NULL,
    FailureMessage NVARCHAR(4000) NULL,
    RowVersion ROWVERSION NOT NULL,

    CONSTRAINT FK_wf_WorkflowInstances_WorkflowDefinitions FOREIGN KEY (WorkflowDefinitionId) REFERENCES wf.WorkflowDefinitions(Id),
    CONSTRAINT FK_wf_WorkflowInstances_WorkflowVersions FOREIGN KEY (WorkflowVersionId) REFERENCES wf.WorkflowVersions(Id),
    CONSTRAINT FK_wf_WorkflowInstances_Parent FOREIGN KEY (ParentWorkflowInstanceId) REFERENCES wf.WorkflowInstances(Id),
    CONSTRAINT FK_wf_WorkflowInstances_Root FOREIGN KEY (RootWorkflowInstanceId) REFERENCES wf.WorkflowInstances(Id),
    CONSTRAINT FK_wf_WorkflowInstances_StartedByUser FOREIGN KEY (StartedByUserId) REFERENCES dbo.AspNetUsers(Id),
    CONSTRAINT CK_wf_WorkflowInstances_Status CHECK (Status IN (N'Pending', N'Running', N'Waiting', N'Completed', N'Cancelled', N'Failed'))
);
GO

CREATE TABLE wf.WorkflowInstanceNodes
(
    Id UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT PK_wf_WorkflowInstanceNodes PRIMARY KEY
        CONSTRAINT DF_wf_WorkflowInstanceNodes_Id DEFAULT NEWSEQUENTIALID(),
    WorkflowInstanceId UNIQUEIDENTIFIER NOT NULL,
    NodeId NVARCHAR(150) NOT NULL,
    NodeType NVARCHAR(80) NOT NULL,
    NodeName NVARCHAR(200) NULL,
    BranchKey NVARCHAR(100) NULL,
    JoinGroupKey NVARCHAR(100) NULL,
    ExecutionStatus NVARCHAR(30) NOT NULL
        CONSTRAINT DF_wf_WorkflowInstanceNodes_ExecutionStatus DEFAULT N'Pending',
    SequenceNo INT NOT NULL
        CONSTRAINT DF_wf_WorkflowInstanceNodes_SequenceNo DEFAULT 0,
    RetryCount INT NOT NULL
        CONSTRAINT DF_wf_WorkflowInstanceNodes_RetryCount DEFAULT 0,
    MaxRetryCount INT NOT NULL
        CONSTRAINT DF_wf_WorkflowInstanceNodes_MaxRetryCount DEFAULT 0,
    ActivatedAtUtc DATETIME2(7) NULL,
    CompletedAtUtc DATETIME2(7) NULL,
    TokenJson NVARCHAR(MAX) NULL,
    ErrorCode NVARCHAR(100) NULL,
    ErrorMessage NVARCHAR(4000) NULL,
    RowVersion ROWVERSION NOT NULL,

    CONSTRAINT FK_wf_WorkflowInstanceNodes_WorkflowInstances FOREIGN KEY (WorkflowInstanceId) REFERENCES wf.WorkflowInstances(Id),
    CONSTRAINT CK_wf_WorkflowInstanceNodes_ExecutionStatus CHECK (ExecutionStatus IN (N'Pending', N'Active', N'Waiting', N'Completed', N'Cancelled', N'Failed'))
);
GO

CREATE TABLE wf.WorkflowInstanceVariables
(
    Id UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT PK_wf_WorkflowInstanceVariables PRIMARY KEY
        CONSTRAINT DF_wf_WorkflowInstanceVariables_Id DEFAULT NEWSEQUENTIALID(),
    WorkflowInstanceId UNIQUEIDENTIFIER NOT NULL,
    VariableName NVARCHAR(200) NOT NULL,
    ValueJson NVARCHAR(MAX) NULL,
    ValueType NVARCHAR(40) NOT NULL,
    CreatedAtUtc DATETIME2(7) NOT NULL
        CONSTRAINT DF_wf_WorkflowInstanceVariables_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc DATETIME2(7) NULL,

    CONSTRAINT FK_wf_WorkflowInstanceVariables_WorkflowInstances FOREIGN KEY (WorkflowInstanceId) REFERENCES wf.WorkflowInstances(Id),
    CONSTRAINT UQ_wf_WorkflowInstanceVariables_InstanceVariable UNIQUE (WorkflowInstanceId, VariableName),
    CONSTRAINT CK_wf_WorkflowInstanceVariables_ValueType CHECK (ValueType IN (N'String', N'Number', N'Boolean', N'DateTime', N'Json', N'Object', N'Array', N'Null'))
);
GO

CREATE TABLE wf.WorkflowTasks
(
    Id UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT PK_wf_WorkflowTasks PRIMARY KEY
        CONSTRAINT DF_wf_WorkflowTasks_Id DEFAULT NEWSEQUENTIALID(),
    TenantId UNIQUEIDENTIFIER NULL,
    WorkflowInstanceId UNIQUEIDENTIFIER NOT NULL,
    WorkflowInstanceNodeId UNIQUEIDENTIFIER NOT NULL,
    TaskCode NVARCHAR(100) NULL,
    NodeId NVARCHAR(150) NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(2000) NULL,
    Status NVARCHAR(30) NOT NULL
        CONSTRAINT DF_wf_WorkflowTasks_Status DEFAULT N'Pending',
    TaskMode NVARCHAR(40) NOT NULL
        CONSTRAINT DF_wf_WorkflowTasks_TaskMode DEFAULT N'approval',
    Priority NVARCHAR(20) NULL,
    EntityType NVARCHAR(80) NULL,
    EntityId NVARCHAR(120) NULL,
    FormKey NVARCHAR(120) NULL,
    ListViewKey NVARCHAR(120) NULL,
    DetailViewKey NVARCHAR(120) NULL,
    AvailableActionsJson NVARCHAR(MAX) NULL,
    DisplayMetadataJson NVARCHAR(MAX) NULL,
    TaskTagsJson NVARCHAR(MAX) NULL,
    AssignmentType NVARCHAR(50) NOT NULL
        CONSTRAINT DF_wf_WorkflowTasks_AssignmentType DEFAULT N'User',
    AssignedToUserId UNIQUEIDENTIFIER NULL,
    AssignedToGroupId UNIQUEIDENTIFIER NULL,
    AssignedToRoleId UNIQUEIDENTIFIER NULL,
    AssignedToDisplayName NVARCHAR(400) NULL,
    ClaimRequired BIT NOT NULL
        CONSTRAINT DF_wf_WorkflowTasks_ClaimRequired DEFAULT 0,
    QueueKey NVARCHAR(120) NULL,
    ClaimedByUserId UNIQUEIDENTIFIER NULL,
    ClaimedBy NVARCHAR(200) NULL,
    ClaimedAtUtc DATETIME2(7) NULL,
    CompletedByUserId UNIQUEIDENTIFIER NULL,
    CompletedBy NVARCHAR(200) NULL,
    CompletedAtUtc DATETIME2(7) NULL,
    DueAtUtc DATETIME2(7) NULL,
    ReminderAtUtc DATETIME2(7) NULL,
    EscalationAtUtc DATETIME2(7) NULL,
    EscalationPolicyKey NVARCHAR(120) NULL,
    SlaStatus NVARCHAR(30) NULL,
    EscalatedAtUtc DATETIME2(7) NULL,
    Outcome NVARCHAR(60) NULL,
    InputPayloadJson NVARCHAR(MAX) NULL,
    OutputPayloadJson NVARCHAR(MAX) NULL,
    Comment NVARCHAR(2000) NULL,
    CreatedAtUtc DATETIME2(7) NOT NULL
        CONSTRAINT DF_wf_WorkflowTasks_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    RowVersion ROWVERSION NOT NULL,

    CONSTRAINT FK_wf_WorkflowTasks_WorkflowInstances FOREIGN KEY (WorkflowInstanceId) REFERENCES wf.WorkflowInstances(Id),
    CONSTRAINT FK_wf_WorkflowTasks_WorkflowInstanceNodes FOREIGN KEY (WorkflowInstanceNodeId) REFERENCES wf.WorkflowInstanceNodes(Id),
    CONSTRAINT FK_wf_WorkflowTasks_AssignedUser FOREIGN KEY (AssignedToUserId) REFERENCES dbo.AspNetUsers(Id),
    CONSTRAINT FK_wf_WorkflowTasks_ClaimedUser FOREIGN KEY (ClaimedByUserId) REFERENCES dbo.AspNetUsers(Id),
    CONSTRAINT FK_wf_WorkflowTasks_CompletedUser FOREIGN KEY (CompletedByUserId) REFERENCES dbo.AspNetUsers(Id),
    CONSTRAINT FK_wf_WorkflowTasks_AssignedGroup FOREIGN KEY (AssignedToGroupId) REFERENCES dbo.PlatformAccessGroups(Id),
    CONSTRAINT FK_wf_WorkflowTasks_AssignedRole FOREIGN KEY (AssignedToRoleId) REFERENCES dbo.PlatformAccessRoles(Id),
    CONSTRAINT CK_wf_WorkflowTasks_Status CHECK (Status IN (N'Pending', N'Claimed', N'Completed', N'Cancelled', N'Escalated')),
    CONSTRAINT CK_wf_WorkflowTasks_AssignmentType CHECK (AssignmentType IN (N'User', N'Group', N'Role', N'Pool', N'Expression')),
    CONSTRAINT CK_wf_WorkflowTasks_TaskMode CHECK (TaskMode IN (N'approval', N'review', N'dataEntry', N'acknowledgement', N'manualAction', N'exception')),
    CONSTRAINT CK_wf_WorkflowTasks_Priority CHECK (Priority IS NULL OR Priority IN (N'Low', N'Medium', N'High', N'Critical')),
    CONSTRAINT CK_wf_WorkflowTasks_SlaStatus CHECK (SlaStatus IS NULL OR SlaStatus IN (N'OnTrack', N'DueSoon', N'Overdue', N'Escalated'))
);
GO

CREATE TABLE wf.WorkflowTaskComments
(
    Id UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT PK_wf_WorkflowTaskComments PRIMARY KEY
        CONSTRAINT DF_wf_WorkflowTaskComments_Id DEFAULT NEWSEQUENTIALID(),
    WorkflowTaskId UNIQUEIDENTIFIER NOT NULL,
    CommentType NVARCHAR(30) NOT NULL,
    Body NVARCHAR(4000) NOT NULL,
    Visibility NVARCHAR(30) NOT NULL
        CONSTRAINT DF_wf_WorkflowTaskComments_Visibility DEFAULT N'Internal',
    CreatedBy NVARCHAR(200) NOT NULL,
    CreatedByUserId UNIQUEIDENTIFIER NULL,
    CreatedAtUtc DATETIME2(7) NOT NULL
        CONSTRAINT DF_wf_WorkflowTaskComments_CreatedAtUtc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_wf_WorkflowTaskComments_WorkflowTasks FOREIGN KEY (WorkflowTaskId) REFERENCES wf.WorkflowTasks(Id),
    CONSTRAINT FK_wf_WorkflowTaskComments_CreatedByUser FOREIGN KEY (CreatedByUserId) REFERENCES dbo.AspNetUsers(Id),
    CONSTRAINT CK_wf_WorkflowTaskComments_CommentType CHECK (CommentType IN (N'Comment', N'Decision', N'SystemNote', N'Escalation', N'AssignmentReason')),
    CONSTRAINT CK_wf_WorkflowTaskComments_Visibility CHECK (Visibility IN (N'Internal', N'Participant', N'Watcher'))
);
GO

CREATE TABLE wf.WorkflowTaskAssignments
(
    Id UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT PK_wf_WorkflowTaskAssignments PRIMARY KEY
        CONSTRAINT DF_wf_WorkflowTaskAssignments_Id DEFAULT NEWSEQUENTIALID(),
    WorkflowTaskId UNIQUEIDENTIFIER NOT NULL,
    ActionType NVARCHAR(30) NOT NULL,
    FromUserId UNIQUEIDENTIFIER NULL,
    FromGroupId UNIQUEIDENTIFIER NULL,
    FromRoleId UNIQUEIDENTIFIER NULL,
    ToUserId UNIQUEIDENTIFIER NULL,
    ToGroupId UNIQUEIDENTIFIER NULL,
    ToRoleId UNIQUEIDENTIFIER NULL,
    Reason NVARCHAR(1000) NULL,
    PerformedBy NVARCHAR(200) NOT NULL,
    PerformedByUserId UNIQUEIDENTIFIER NULL,
    CreatedAtUtc DATETIME2(7) NOT NULL
        CONSTRAINT DF_wf_WorkflowTaskAssignments_CreatedAtUtc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_wf_WorkflowTaskAssignments_WorkflowTasks FOREIGN KEY (WorkflowTaskId) REFERENCES wf.WorkflowTasks(Id),
    CONSTRAINT FK_wf_WorkflowTaskAssignments_FromUser FOREIGN KEY (FromUserId) REFERENCES dbo.AspNetUsers(Id),
    CONSTRAINT FK_wf_WorkflowTaskAssignments_ToUser FOREIGN KEY (ToUserId) REFERENCES dbo.AspNetUsers(Id),
    CONSTRAINT FK_wf_WorkflowTaskAssignments_PerformedByUser FOREIGN KEY (PerformedByUserId) REFERENCES dbo.AspNetUsers(Id),
    CONSTRAINT FK_wf_WorkflowTaskAssignments_FromGroup FOREIGN KEY (FromGroupId) REFERENCES dbo.PlatformAccessGroups(Id),
    CONSTRAINT FK_wf_WorkflowTaskAssignments_ToGroup FOREIGN KEY (ToGroupId) REFERENCES dbo.PlatformAccessGroups(Id),
    CONSTRAINT FK_wf_WorkflowTaskAssignments_FromRole FOREIGN KEY (FromRoleId) REFERENCES dbo.PlatformAccessRoles(Id),
    CONSTRAINT FK_wf_WorkflowTaskAssignments_ToRole FOREIGN KEY (ToRoleId) REFERENCES dbo.PlatformAccessRoles(Id),
    CONSTRAINT CK_wf_WorkflowTaskAssignments_ActionType CHECK (ActionType IN (N'Assigned', N'Claimed', N'Unclaimed', N'Delegated', N'Reassigned', N'Escalated', N'AutoRouted'))
);
GO

CREATE TABLE wf.WorkflowTimers
(
    Id UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT PK_wf_WorkflowTimers PRIMARY KEY
        CONSTRAINT DF_wf_WorkflowTimers_Id DEFAULT NEWSEQUENTIALID(),
    WorkflowInstanceId UNIQUEIDENTIFIER NOT NULL,
    WorkflowInstanceNodeId UNIQUEIDENTIFIER NOT NULL,
    TimerType NVARCHAR(40) NOT NULL,
    Status NVARCHAR(30) NOT NULL
        CONSTRAINT DF_wf_WorkflowTimers_Status DEFAULT N'Pending',
    DueAtUtc DATETIME2(7) NOT NULL,
    ProcessedAtUtc DATETIME2(7) NULL,
    PayloadJson NVARCHAR(MAX) NULL,
    CreatedAtUtc DATETIME2(7) NOT NULL
        CONSTRAINT DF_wf_WorkflowTimers_CreatedAtUtc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_wf_WorkflowTimers_WorkflowInstances FOREIGN KEY (WorkflowInstanceId) REFERENCES wf.WorkflowInstances(Id),
    CONSTRAINT FK_wf_WorkflowTimers_WorkflowInstanceNodes FOREIGN KEY (WorkflowInstanceNodeId) REFERENCES wf.WorkflowInstanceNodes(Id),
    CONSTRAINT CK_wf_WorkflowTimers_Status CHECK (Status IN (N'Pending', N'Processed', N'Cancelled', N'Failed'))
);
GO

CREATE TABLE wf.WorkflowEventSubscriptions
(
    Id UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT PK_wf_WorkflowEventSubscriptions PRIMARY KEY
        CONSTRAINT DF_wf_WorkflowEventSubscriptions_Id DEFAULT NEWSEQUENTIALID(),
    WorkflowInstanceId UNIQUEIDENTIFIER NOT NULL,
    WorkflowInstanceNodeId UNIQUEIDENTIFIER NOT NULL,
    EventName NVARCHAR(150) NOT NULL,
    CorrelationKey NVARCHAR(200) NULL,
    Status NVARCHAR(30) NOT NULL
        CONSTRAINT DF_wf_WorkflowEventSubscriptions_Status DEFAULT N'Waiting',
    PayloadSchemaJson NVARCHAR(MAX) NULL,
    ExpiresAtUtc DATETIME2(7) NULL,
    SatisfiedAtUtc DATETIME2(7) NULL,
    CreatedAtUtc DATETIME2(7) NOT NULL
        CONSTRAINT DF_wf_WorkflowEventSubscriptions_CreatedAtUtc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_wf_WorkflowEventSubscriptions_WorkflowInstances FOREIGN KEY (WorkflowInstanceId) REFERENCES wf.WorkflowInstances(Id),
    CONSTRAINT FK_wf_WorkflowEventSubscriptions_WorkflowInstanceNodes FOREIGN KEY (WorkflowInstanceNodeId) REFERENCES wf.WorkflowInstanceNodes(Id),
    CONSTRAINT CK_wf_WorkflowEventSubscriptions_Status CHECK (Status IN (N'Waiting', N'Satisfied', N'Expired', N'Cancelled'))
);
GO

CREATE TABLE wf.WorkflowExecutionLogs
(
    Id BIGINT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_wf_WorkflowExecutionLogs PRIMARY KEY,
    WorkflowInstanceId UNIQUEIDENTIFIER NOT NULL,
    WorkflowInstanceNodeId UNIQUEIDENTIFIER NULL,
    LogLevel NVARCHAR(20) NOT NULL,
    EventType NVARCHAR(100) NOT NULL,
    Message NVARCHAR(2000) NULL,
    DataJson NVARCHAR(MAX) NULL,
    CreatedAtUtc DATETIME2(7) NOT NULL
        CONSTRAINT DF_wf_WorkflowExecutionLogs_CreatedAtUtc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_wf_WorkflowExecutionLogs_WorkflowInstances FOREIGN KEY (WorkflowInstanceId) REFERENCES wf.WorkflowInstances(Id),
    CONSTRAINT FK_wf_WorkflowExecutionLogs_WorkflowInstanceNodes FOREIGN KEY (WorkflowInstanceNodeId) REFERENCES wf.WorkflowInstanceNodes(Id),
    CONSTRAINT CK_wf_WorkflowExecutionLogs_LogLevel CHECK (LogLevel IN (N'Trace', N'Debug', N'Info', N'Warn', N'Error', N'Fatal'))
);
GO

CREATE TABLE wf.WorkflowAuditLogs
(
    Id BIGINT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_wf_WorkflowAuditLogs PRIMARY KEY,
    WorkflowInstanceId UNIQUEIDENTIFIER NOT NULL,
    WorkflowTaskId UNIQUEIDENTIFIER NULL,
    ActorType NVARCHAR(40) NOT NULL,
    ActorId NVARCHAR(200) NULL,
    ActorUserId UNIQUEIDENTIFIER NULL,
    Action NVARCHAR(100) NOT NULL,
    Summary NVARCHAR(1000) NULL,
    FromNodeId NVARCHAR(150) NULL,
    ToNodeId NVARCHAR(150) NULL,
    DataJson NVARCHAR(MAX) NULL,
    CreatedAtUtc DATETIME2(7) NOT NULL
        CONSTRAINT DF_wf_WorkflowAuditLogs_CreatedAtUtc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_wf_WorkflowAuditLogs_WorkflowInstances FOREIGN KEY (WorkflowInstanceId) REFERENCES wf.WorkflowInstances(Id),
    CONSTRAINT FK_wf_WorkflowAuditLogs_WorkflowTasks FOREIGN KEY (WorkflowTaskId) REFERENCES wf.WorkflowTasks(Id),
    CONSTRAINT FK_wf_WorkflowAuditLogs_ActorUser FOREIGN KEY (ActorUserId) REFERENCES dbo.AspNetUsers(Id),
    CONSTRAINT CK_wf_WorkflowAuditLogs_ActorType CHECK (ActorType IN (N'User', N'System', N'Runtime', N'API', N'Integration'))
);
GO

CREATE TABLE wf.WorkflowOutbox
(
    Id BIGINT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_wf_WorkflowOutbox PRIMARY KEY,
    AggregateType NVARCHAR(80) NOT NULL,
    AggregateId NVARCHAR(200) NOT NULL,
    EventType NVARCHAR(120) NOT NULL,
    PayloadJson NVARCHAR(MAX) NOT NULL,
    OccurredAtUtc DATETIME2(7) NOT NULL
        CONSTRAINT DF_wf_WorkflowOutbox_OccurredAtUtc DEFAULT SYSUTCDATETIME(),
    ProcessedAtUtc DATETIME2(7) NULL,
    RetryCount INT NOT NULL
        CONSTRAINT DF_wf_WorkflowOutbox_RetryCount DEFAULT 0,
    LastAttemptAtUtc DATETIME2(7) NULL,
    NextAttemptAtUtc DATETIME2(7) NULL,
    DeadLetteredAtUtc DATETIME2(7) NULL,
    ProcessorName NVARCHAR(120) NULL,
    ErrorCode NVARCHAR(100) NULL,
    ErrorMessage NVARCHAR(2000) NULL,
    HeadersJson NVARCHAR(MAX) NULL,
    Status NVARCHAR(30) NOT NULL
        CONSTRAINT DF_wf_WorkflowOutbox_Status DEFAULT N'Pending',
    LockId UNIQUEIDENTIFIER NULL,
    LockedAtUtc DATETIME2(7) NULL,

    CONSTRAINT CK_wf_WorkflowOutbox_Status CHECK (Status IN (N'Pending', N'Processing', N'Processed', N'Failed', N'DeadLettered'))
);
GO

/* Indexes */
CREATE INDEX IX_wf_WorkflowDefinitions_Status ON wf.WorkflowDefinitions(Status, UpdatedAtUtc);
GO
CREATE INDEX IX_wf_WorkflowDrafts_ValidationStatus ON wf.WorkflowDrafts(ValidationStatus, UpdatedAtUtc);
GO
CREATE INDEX IX_wf_WorkflowVersions_DefinitionVersion ON wf.WorkflowVersions(WorkflowDefinitionId, VersionNumber DESC);
GO
CREATE INDEX IX_wf_WorkflowVersions_EffectiveWindow ON wf.WorkflowVersions(Status, EffectiveFromUtc, EffectiveToUtc);
GO
CREATE INDEX IX_wf_WorkflowInstances_Status ON wf.WorkflowInstances(Status, LastHeartbeatUtc, StartedAtUtc);
GO
CREATE INDEX IX_wf_WorkflowInstances_BusinessKey ON wf.WorkflowInstances(BusinessKey) WHERE BusinessKey IS NOT NULL;
GO
CREATE INDEX IX_wf_WorkflowInstances_CorrelationId ON wf.WorkflowInstances(CorrelationId) WHERE CorrelationId IS NOT NULL;
GO
CREATE INDEX IX_wf_WorkflowInstanceNodes_InstanceStatus ON wf.WorkflowInstanceNodes(WorkflowInstanceId, ExecutionStatus, ActivatedAtUtc);
GO
CREATE INDEX IX_wf_WorkflowInstanceVariables_Instance ON wf.WorkflowInstanceVariables(WorkflowInstanceId, VariableName);
GO
CREATE INDEX IX_wf_WorkflowTasks_StatusDue ON wf.WorkflowTasks(Status, DueAtUtc, CreatedAtUtc);
GO
CREATE INDEX IX_wf_WorkflowTasks_AssignedUser_Status ON wf.WorkflowTasks(AssignedToUserId, Status, DueAtUtc) WHERE AssignedToUserId IS NOT NULL;
GO
CREATE INDEX IX_wf_WorkflowTasks_AssignedGroup_Status ON wf.WorkflowTasks(AssignedToGroupId, Status, DueAtUtc) WHERE AssignedToGroupId IS NOT NULL;
GO
CREATE INDEX IX_wf_WorkflowTasks_AssignedRole_Status ON wf.WorkflowTasks(AssignedToRoleId, Status, DueAtUtc) WHERE AssignedToRoleId IS NOT NULL;
GO
CREATE INDEX IX_wf_WorkflowTasks_StatusPriorityDue ON wf.WorkflowTasks(Status, Priority, DueAtUtc, CreatedAtUtc);
GO
CREATE INDEX IX_wf_WorkflowTasks_TaskModeStatus ON wf.WorkflowTasks(TaskMode, Status, DueAtUtc);
GO
CREATE INDEX IX_wf_WorkflowTasks_Entity ON wf.WorkflowTasks(EntityType, EntityId) WHERE EntityType IS NOT NULL AND EntityId IS NOT NULL;
GO
CREATE INDEX IX_wf_WorkflowTasks_Escalation ON wf.WorkflowTasks(Status, EscalationAtUtc) WHERE EscalationAtUtc IS NOT NULL;
GO
CREATE INDEX IX_wf_WorkflowTasks_Reminder ON wf.WorkflowTasks(Status, ReminderAtUtc) WHERE ReminderAtUtc IS NOT NULL;
GO
CREATE INDEX IX_wf_WorkflowTaskComments_TaskCreated ON wf.WorkflowTaskComments(WorkflowTaskId, CreatedAtUtc DESC);
GO
CREATE INDEX IX_wf_WorkflowTaskAssignments_TaskCreated ON wf.WorkflowTaskAssignments(WorkflowTaskId, CreatedAtUtc DESC);
GO
CREATE INDEX IX_wf_WorkflowTaskAssignments_PerformedBy ON wf.WorkflowTaskAssignments(PerformedByUserId, CreatedAtUtc DESC) WHERE PerformedByUserId IS NOT NULL;
GO
CREATE INDEX IX_wf_WorkflowTimers_StatusDue ON wf.WorkflowTimers(Status, DueAtUtc);
GO
CREATE INDEX IX_wf_WorkflowEventSubscriptions_StatusEvent ON wf.WorkflowEventSubscriptions(Status, EventName, CorrelationKey, ExpiresAtUtc);
GO
CREATE INDEX IX_wf_WorkflowExecutionLogs_InstanceCreated ON wf.WorkflowExecutionLogs(WorkflowInstanceId, CreatedAtUtc DESC);
GO
CREATE INDEX IX_wf_WorkflowAuditLogs_InstanceCreated ON wf.WorkflowAuditLogs(WorkflowInstanceId, CreatedAtUtc DESC);
GO
CREATE INDEX IX_wf_WorkflowOutbox_StatusNextAttempt ON wf.WorkflowOutbox(Status, NextAttemptAtUtc, OccurredAtUtc, RetryCount);
GO
CREATE INDEX IX_wf_WorkflowOutbox_LockedAt ON wf.WorkflowOutbox(Status, LockedAtUtc) WHERE LockedAtUtc IS NOT NULL;
GO
