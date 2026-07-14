namespace Nodify.Avalonia.Workflow.Designer
{
    public sealed class WorkflowStepConnectionViewModel
    {
        public WorkflowStepConnectionViewModel(WorkflowStepViewModel from, WorkflowStepViewModel to)
        {
            From = from;
            To = to;
        }

        public WorkflowStepViewModel From { get; }
        public WorkflowStepViewModel To { get; }
    }
}
