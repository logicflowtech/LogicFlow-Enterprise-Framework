using LogicFlowEnterpriseFramework.Blazor.Components.Pages;
using LogicFlowEnterpriseFramework.Shared.Constants;
using LogicFlowEnterpriseFramework.Shared.Features;
using Microsoft.Extensions.DependencyInjection;

namespace LogicFlowEnterpriseFramework.Blazor.Features.Workflow;

public sealed class WorkflowFeature : IPlatformFeature
{
    public string FeatureCode => PlatformFeatureCodes.Workflow;
    public string Title => "Workflow";
    public string Description => "Workflow design, release, and operational management.";
    public string Icon => "tasks";
    public string BasePath => "/workflow/definitions";
    public int SortOrder => 15;
    public bool IsEnabled => true;

    public void RegisterServices(IServiceCollection services)
    {
    }

    public void RegisterMenus(IMenuRegistry menuRegistry)
    {
        menuRegistry.Register(new PlatformMenuDefinition("workflow.definitions", FeatureCode, "Workflow", "/workflow/definitions", "tasks", 44, "workspace.settings"));
        menuRegistry.Register(new PlatformMenuDefinition("workflow.operations", FeatureCode, "Workflow Operations", "/workflow/operations", "assignment", 45, "workspace.settings"));
    }

    public void RegisterPermissions(IPermissionRegistry permissionRegistry)
    {
        permissionRegistry.Register(new PlatformPermissionDefinition(FeatureCode, Permissions.WorkflowRead, "Workflow Read", "View workflow definitions and runtime data."));
        permissionRegistry.Register(new PlatformPermissionDefinition(FeatureCode, Permissions.WorkflowAdmin, "Workflow Admin", "Create, edit, and publish workflow definitions."));
        permissionRegistry.Register(new PlatformPermissionDefinition(FeatureCode, Permissions.WorkflowOperate, "Workflow Operate", "Operate assigned workflow tasks."));
    }

    public void RegisterPages(IPageRegistry pageRegistry)
    {
        pageRegistry.Register(new PlatformPageDefinition(FeatureCode, typeof(WorkflowDesignerHost), "/workflow/definitions", "Workflow", "workflow.definitions"));
        pageRegistry.Register(new PlatformPageDefinition(FeatureCode, typeof(WorkflowOperations), "/workflow/operations", "Workflow Operations", "workflow.operations"));
        pageRegistry.Register(new PlatformPageDefinition(FeatureCode, typeof(WorkflowStandaloneDesignerHost), "/workflow/designer/{DefinitionId}", "Workflow Designer", null));
    }
}
