﻿using System.Activities.Hosting;
using System.Activities.Runtime;
using System.Activities.Runtime.Collections;
using System.Collections.ObjectModel;
namespace System.Activities.Statements;
public class BpmJoin : BpmNode
{
    ValidatingCollection<BpmNode> _branches;
    [DefaultValue(null)]
    public Collection<BpmNode> Branches => _branches ??= ValidatingCollection<BpmNode>.NullCheck();
    [DefaultValue(null)]
    public BpmNode Next { get; set; }
    protected override bool CanInduceIdle => true;
    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        metadata.RequireExtension<BookmarkResumptionHelper>();
        metadata.AddDefaultExtensionProvider(() => new BookmarkResumptionHelper());
    }
    record JoinState
    {
        public int Count;
    }
    protected override void Execute(NativeActivityContext context)
    {
        var key = $"{nameof(BpmJoin)}_{Id}";
        Dictionary<string, object> state;
        using (context.InheritVariables())
        {
            state = context.GetValue<Dictionary<string, object>>("flowchartState");
        }
        var joinState = (JoinState)state.GetValueOrDefault(key);
        if (joinState == null)
        {
            joinState = new() { Count = 1 };
            state.Add(key, joinState);
            context.CreateBookmark(key, OnBookmarkResumed);
        }
        else
        {
            joinState.Count++;
        }
        if (joinState.Count == Branches.Count)
        {
            state.Remove(key);
            var bookmarkHelper = context.GetExtension<BookmarkResumptionHelper>();
            bookmarkHelper.ResumeBookmark(new Bookmark(key));
            TryExecute(Next, context, context.CurrentInstance);
        }
    }
    static void OnBookmarkResumed(NativeActivityContext context, Bookmark bookmark, object value) { }
    internal override void GetConnectedNodes(IList<BpmNode> connections)
    {
        if (Next != null)
        {
            connections.Add(Next);
        }
    }
}
internal sealed class BookmarkResumptionHelper : IWorkflowInstanceExtension
{
    private WorkflowInstanceProxy _instance;
    public void ResumeBookmark(Bookmark bookmark)
    {
        var asyncResult = _instance.BeginResumeBookmark(bookmark, null, Fx.ThunkCallback(OnResumeBookmarkCompleted), _instance);
        if (asyncResult.CompletedSynchronously)
        {
            _instance.EndResumeBookmark(asyncResult);
        }
    }
    private static void OnResumeBookmarkCompleted(IAsyncResult result)
    {
        if (!result.CompletedSynchronously)
        {
            WorkflowInstanceProxy instance = result.AsyncState as WorkflowInstanceProxy;
            Fx.Assert(instance != null, "BeginResumeBookmark should pass a WorkflowInstanceProxy object as the async state object.");
            instance.EndResumeBookmark(result);
        }
    }
    IEnumerable<object> IWorkflowInstanceExtension.GetAdditionalExtensions() => null;
    void IWorkflowInstanceExtension.SetInstance(WorkflowInstanceProxy instance) => _instance = instance;
}