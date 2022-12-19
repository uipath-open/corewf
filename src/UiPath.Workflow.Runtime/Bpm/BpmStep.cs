using System.Windows.Markup;
namespace System.Activities.Statements;
[ContentProperty("Action")]
public sealed class BpmStep : BpmNode
{
    private CompletionCallback _onCompleted;
    [DefaultValue(null)]
    public Activity Action { get; set; }
    [DefaultValue(null)]
    [DependsOn("Action")]
    public BpmNode Next { get; set; }
    internal override void OnOpen(BpmFlowchart owner, NativeActivityMetadata metadata) { }
    internal override void GetConnectedNodes(IList<BpmNode> connections)
    {
        if (Next != null)
        {
            connections.Add(Next);
        }
    }
    protected override void Execute(NativeActivityContext context)
    {
        if (Next == null)
        {
            if (TD.FlowchartNextNullIsEnabled())
            {
                TD.FlowchartNextNull(Owner.DisplayName);
            }
        }
        if (Action == null)
        {
            OnCompleted(context, null);
        }
        else
        {
            _onCompleted ??= new(OnCompleted);
            context.ScheduleActivity(Action, _onCompleted);
        }
    }
    private void OnCompleted(NativeActivityContext context, ActivityInstance completedInstance) => Next.TryExecute(context, this, completedInstance);
}