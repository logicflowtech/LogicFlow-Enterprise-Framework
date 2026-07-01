namespace LogicFlowEnterpriseFramework.Api.Workflow.Runtime;

public sealed class WorkflowBackgroundProcessingOptions
{
    public const string SectionName = "WorkflowBackgroundProcessing";

    public bool Enabled { get; set; } = true;
    public int PollIntervalSeconds { get; set; } = 30;
    public int TimerBatchSize { get; set; } = 25;
    public int OutboxBatchSize { get; set; } = 50;
}
