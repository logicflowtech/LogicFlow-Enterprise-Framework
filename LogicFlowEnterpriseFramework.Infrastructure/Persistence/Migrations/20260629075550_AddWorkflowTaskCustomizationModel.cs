using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFlowEnterpriseFramework.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowTaskCustomizationModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TaskMode",
                schema: "wf",
                table: "WorkflowTasks",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "approval");

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                schema: "wf",
                table: "WorkflowTasks",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityType",
                schema: "wf",
                table: "WorkflowTasks",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityId",
                schema: "wf",
                table: "WorkflowTasks",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormKey",
                schema: "wf",
                table: "WorkflowTasks",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ListViewKey",
                schema: "wf",
                table: "WorkflowTasks",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DetailViewKey",
                schema: "wf",
                table: "WorkflowTasks",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvailableActionsJson",
                schema: "wf",
                table: "WorkflowTasks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayMetadataJson",
                schema: "wf",
                table: "WorkflowTasks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaskTagsJson",
                schema: "wf",
                table: "WorkflowTasks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ClaimRequired",
                schema: "wf",
                table: "WorkflowTasks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "QueueKey",
                schema: "wf",
                table: "WorkflowTasks",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReminderAtUtc",
                schema: "wf",
                table: "WorkflowTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EscalationAtUtc",
                schema: "wf",
                table: "WorkflowTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EscalationPolicyKey",
                schema: "wf",
                table: "WorkflowTasks",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SlaStatus",
                schema: "wf",
                table: "WorkflowTasks",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EscalatedAtUtc",
                schema: "wf",
                table: "WorkflowTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAttemptAtUtc",
                schema: "wf",
                table: "WorkflowOutbox",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextAttemptAtUtc",
                schema: "wf",
                table: "WorkflowOutbox",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeadLetteredAtUtc",
                schema: "wf",
                table: "WorkflowOutbox",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessorName",
                schema: "wf",
                table: "WorkflowOutbox",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorCode",
                schema: "wf",
                table: "WorkflowOutbox",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeadersJson",
                schema: "wf",
                table: "WorkflowOutbox",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkflowTaskAssignments",
                schema: "wf",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FromUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FromGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FromRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ToGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ToRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PerformedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PerformedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTaskAssignments", x => x.Id);
                    table.CheckConstraint("CK_wf_WorkflowTaskAssignments_ActionType", "[ActionType] IN (N'Assigned', N'Claimed', N'Unclaimed', N'Delegated', N'Reassigned', N'Escalated', N'AutoRouted')");
                    table.ForeignKey(
                        name: "FK_WorkflowTaskAssignments_AspNetUsers_FromUserId",
                        column: x => x.FromUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkflowTaskAssignments_AspNetUsers_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkflowTaskAssignments_AspNetUsers_ToUserId",
                        column: x => x.ToUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkflowTaskAssignments_PlatformAccessGroups_FromGroupId",
                        column: x => x.FromGroupId,
                        principalTable: "PlatformAccessGroups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkflowTaskAssignments_PlatformAccessGroups_ToGroupId",
                        column: x => x.ToGroupId,
                        principalTable: "PlatformAccessGroups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkflowTaskAssignments_PlatformAccessRoles_FromRoleId",
                        column: x => x.FromRoleId,
                        principalTable: "PlatformAccessRoles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkflowTaskAssignments_PlatformAccessRoles_ToRoleId",
                        column: x => x.ToRoleId,
                        principalTable: "PlatformAccessRoles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkflowTaskAssignments_WorkflowTasks_WorkflowTaskId",
                        column: x => x.WorkflowTaskId,
                        principalSchema: "wf",
                        principalTable: "WorkflowTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowTaskComments",
                schema: "wf",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommentType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Visibility = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTaskComments", x => x.Id);
                    table.CheckConstraint("CK_wf_WorkflowTaskComments_CommentType", "[CommentType] IN (N'Comment', N'Decision', N'SystemNote', N'Escalation', N'AssignmentReason')");
                    table.CheckConstraint("CK_wf_WorkflowTaskComments_Visibility", "[Visibility] IN (N'Internal', N'Participant', N'Watcher')");
                    table.ForeignKey(
                        name: "FK_WorkflowTaskComments_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkflowTaskComments_WorkflowTasks_WorkflowTaskId",
                        column: x => x.WorkflowTaskId,
                        principalSchema: "wf",
                        principalTable: "WorkflowTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                ALTER TABLE [wf].[WorkflowOutbox] DROP CONSTRAINT [CK_wf_WorkflowOutbox_Status];
                ALTER TABLE [wf].[WorkflowOutbox] ADD CONSTRAINT [CK_wf_WorkflowOutbox_Status] CHECK ([Status] IN (N'Pending', N'Processing', N'Processed', N'Failed', N'DeadLettered'));
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_wf_WorkflowTasks_TaskMode",
                schema: "wf",
                table: "WorkflowTasks",
                sql: "[TaskMode] IN (N'approval', N'review', N'dataEntry', N'acknowledgement', N'manualAction', N'exception')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_wf_WorkflowTasks_Priority",
                schema: "wf",
                table: "WorkflowTasks",
                sql: "[Priority] IS NULL OR [Priority] IN (N'Low', N'Medium', N'High', N'Critical')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_wf_WorkflowTasks_SlaStatus",
                schema: "wf",
                table: "WorkflowTasks",
                sql: "[SlaStatus] IS NULL OR [SlaStatus] IN (N'OnTrack', N'DueSoon', N'Overdue', N'Escalated')");

            migrationBuilder.CreateIndex(
                name: "IX_wf_WorkflowOutbox_LockedAt",
                schema: "wf",
                table: "WorkflowOutbox",
                columns: new[] { "Status", "LockedAtUtc" },
                filter: "[LockedAtUtc] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_wf_WorkflowOutbox_StatusNextAttempt",
                schema: "wf",
                table: "WorkflowOutbox",
                columns: new[] { "Status", "NextAttemptAtUtc", "OccurredAtUtc", "RetryCount" });

            migrationBuilder.CreateIndex(
                name: "IX_wf_WorkflowTaskAssignments_PerformedBy",
                schema: "wf",
                table: "WorkflowTaskAssignments",
                columns: new[] { "PerformedByUserId", "CreatedAtUtc" },
                filter: "[PerformedByUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_wf_WorkflowTaskAssignments_TaskCreated",
                schema: "wf",
                table: "WorkflowTaskAssignments",
                columns: new[] { "WorkflowTaskId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTaskAssignments_FromGroupId",
                schema: "wf",
                table: "WorkflowTaskAssignments",
                column: "FromGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTaskAssignments_FromRoleId",
                schema: "wf",
                table: "WorkflowTaskAssignments",
                column: "FromRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTaskAssignments_FromUserId",
                schema: "wf",
                table: "WorkflowTaskAssignments",
                column: "FromUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTaskAssignments_PerformedByUserId",
                schema: "wf",
                table: "WorkflowTaskAssignments",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTaskAssignments_ToGroupId",
                schema: "wf",
                table: "WorkflowTaskAssignments",
                column: "ToGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTaskAssignments_ToRoleId",
                schema: "wf",
                table: "WorkflowTaskAssignments",
                column: "ToRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTaskAssignments_ToUserId",
                schema: "wf",
                table: "WorkflowTaskAssignments",
                column: "ToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_wf_WorkflowTaskComments_TaskCreated",
                schema: "wf",
                table: "WorkflowTaskComments",
                columns: new[] { "WorkflowTaskId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTaskComments_CreatedByUserId",
                schema: "wf",
                table: "WorkflowTaskComments",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_wf_WorkflowTasks_Entity",
                schema: "wf",
                table: "WorkflowTasks",
                columns: new[] { "EntityType", "EntityId" },
                filter: "[EntityType] IS NOT NULL AND [EntityId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_wf_WorkflowTasks_Escalation",
                schema: "wf",
                table: "WorkflowTasks",
                columns: new[] { "Status", "EscalationAtUtc" },
                filter: "[EscalationAtUtc] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_wf_WorkflowTasks_Reminder",
                schema: "wf",
                table: "WorkflowTasks",
                columns: new[] { "Status", "ReminderAtUtc" },
                filter: "[ReminderAtUtc] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_wf_WorkflowTasks_StatusPriorityDue",
                schema: "wf",
                table: "WorkflowTasks",
                columns: new[] { "Status", "Priority", "DueAtUtc", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_wf_WorkflowTasks_TaskModeStatus",
                schema: "wf",
                table: "WorkflowTasks",
                columns: new[] { "TaskMode", "Status", "DueAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowTaskAssignments",
                schema: "wf");

            migrationBuilder.DropTable(
                name: "WorkflowTaskComments",
                schema: "wf");

            migrationBuilder.DropIndex(
                name: "IX_wf_WorkflowOutbox_LockedAt",
                schema: "wf",
                table: "WorkflowOutbox");

            migrationBuilder.DropIndex(
                name: "IX_wf_WorkflowOutbox_StatusNextAttempt",
                schema: "wf",
                table: "WorkflowOutbox");

            migrationBuilder.DropIndex(
                name: "IX_wf_WorkflowTasks_Entity",
                schema: "wf",
                table: "WorkflowTasks");

            migrationBuilder.DropIndex(
                name: "IX_wf_WorkflowTasks_Escalation",
                schema: "wf",
                table: "WorkflowTasks");

            migrationBuilder.DropIndex(
                name: "IX_wf_WorkflowTasks_Reminder",
                schema: "wf",
                table: "WorkflowTasks");

            migrationBuilder.DropIndex(
                name: "IX_wf_WorkflowTasks_StatusPriorityDue",
                schema: "wf",
                table: "WorkflowTasks");

            migrationBuilder.DropIndex(
                name: "IX_wf_WorkflowTasks_TaskModeStatus",
                schema: "wf",
                table: "WorkflowTasks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_wf_WorkflowTasks_TaskMode",
                schema: "wf",
                table: "WorkflowTasks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_wf_WorkflowTasks_Priority",
                schema: "wf",
                table: "WorkflowTasks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_wf_WorkflowTasks_SlaStatus",
                schema: "wf",
                table: "WorkflowTasks");

            migrationBuilder.DropColumn(name: "TaskMode", schema: "wf", table: "WorkflowTasks");
            migrationBuilder.DropColumn(name: "Priority", schema: "wf", table: "WorkflowTasks");
            migrationBuilder.DropColumn(name: "EntityType", schema: "wf", table: "WorkflowTasks");
            migrationBuilder.DropColumn(name: "EntityId", schema: "wf", table: "WorkflowTasks");
            migrationBuilder.DropColumn(name: "FormKey", schema: "wf", table: "WorkflowTasks");
            migrationBuilder.DropColumn(name: "ListViewKey", schema: "wf", table: "WorkflowTasks");
            migrationBuilder.DropColumn(name: "DetailViewKey", schema: "wf", table: "WorkflowTasks");
            migrationBuilder.DropColumn(name: "AvailableActionsJson", schema: "wf", table: "WorkflowTasks");
            migrationBuilder.DropColumn(name: "DisplayMetadataJson", schema: "wf", table: "WorkflowTasks");
            migrationBuilder.DropColumn(name: "TaskTagsJson", schema: "wf", table: "WorkflowTasks");
            migrationBuilder.DropColumn(name: "ClaimRequired", schema: "wf", table: "WorkflowTasks");
            migrationBuilder.DropColumn(name: "QueueKey", schema: "wf", table: "WorkflowTasks");
            migrationBuilder.DropColumn(name: "ReminderAtUtc", schema: "wf", table: "WorkflowTasks");
            migrationBuilder.DropColumn(name: "EscalationAtUtc", schema: "wf", table: "WorkflowTasks");
            migrationBuilder.DropColumn(name: "EscalationPolicyKey", schema: "wf", table: "WorkflowTasks");
            migrationBuilder.DropColumn(name: "SlaStatus", schema: "wf", table: "WorkflowTasks");
            migrationBuilder.DropColumn(name: "EscalatedAtUtc", schema: "wf", table: "WorkflowTasks");

            migrationBuilder.DropColumn(name: "LastAttemptAtUtc", schema: "wf", table: "WorkflowOutbox");
            migrationBuilder.DropColumn(name: "NextAttemptAtUtc", schema: "wf", table: "WorkflowOutbox");
            migrationBuilder.DropColumn(name: "DeadLetteredAtUtc", schema: "wf", table: "WorkflowOutbox");
            migrationBuilder.DropColumn(name: "ProcessorName", schema: "wf", table: "WorkflowOutbox");
            migrationBuilder.DropColumn(name: "ErrorCode", schema: "wf", table: "WorkflowOutbox");
            migrationBuilder.DropColumn(name: "HeadersJson", schema: "wf", table: "WorkflowOutbox");

            migrationBuilder.Sql("""
                ALTER TABLE [wf].[WorkflowOutbox] DROP CONSTRAINT [CK_wf_WorkflowOutbox_Status];
                ALTER TABLE [wf].[WorkflowOutbox] ADD CONSTRAINT [CK_wf_WorkflowOutbox_Status] CHECK ([Status] IN (N'Pending', N'Processing', N'Processed', N'Failed'));
                """);
        }
    }
}
