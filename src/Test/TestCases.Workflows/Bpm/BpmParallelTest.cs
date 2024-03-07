﻿using Shouldly;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Threading;
using Xunit;
using System.Linq;
using System.Activities.Expressions;

namespace TestCases.Activitiess.Bpm;
public class BpmParallelTest
{
    private readonly List<string> _strings = new();
    private readonly Variable<ICollection<string>> _stringsVariable;

    public BpmParallelTest()
    {
        _stringsVariable = new("strings", c => _strings);
    }

    private AddToCollection<string> AddString(string stringToAdd)
        => new() { Collection = _stringsVariable, Item = stringToAdd };

    private void ExecuteFlowchart(FlowNode startNode)
    {
        var flowchart = new Flowchart { StartNode = startNode, Variables = { _stringsVariable } };
        WorkflowInvoker.Invoke(flowchart);
    }

    [Fact]
    public void Should_execute_branches()
    {
        var branch1Str = "branch1";
        var branch2Str = "branch2";
        var parallel = new FlowParallel().AddBranches(AddString(branch1Str), AddString(branch2Str));
        ExecuteFlowchart(parallel);
        _strings.ShouldBe(new() { branch1Str, branch2Str });
    }

    [Fact]
    public void Should_join_branches()
    {
        var branch1Str = "branch1";
        var branch2Str = "branch2";
        var stopString = "stop";

        var parallel = new FlowParallel()
            .AddBranches(
                AddString(branch1Str),
                AddString(branch2Str)
                );
        parallel.JoinNode.FlowTo(AddString(stopString));

        ExecuteFlowchart(parallel);
        _strings.ShouldBe(new() { branch1Str, branch2Str, stopString });
    }

    [Fact]
    public void Should_join_with_skiped_branches()
    {
        var branch1Str = "branch1";
        var branch2Str = "branch2";
        var stopString = "stop";

        var parallel = new FlowParallel()
            .AddBranches(
                AddString(branch1Str),
                AddString(branch2Str)
                );
        parallel.Branches.First().Condition = new LambdaValue<bool>(c => false);
        parallel.JoinNode.FlowTo(AddString(stopString));

        ExecuteFlowchart(parallel);
        _strings.ShouldBe(new() { branch2Str, stopString });
    }

    [Fact]
    public void Should_join_branches_when_condition_is_met()
    {
        var branch1Str = "branch1";
        var branch2Str = "branch2";
        var stopString = "stop";

        var parallel = new FlowParallel()
            .AddBranches(
                AddString(branch1Str),
                AddString(branch2Str)
                );
        parallel.JoinNode.FlowTo(AddString(stopString));
        parallel.JoinNode.Completion = new LambdaValue<bool>(c => true);

        ExecuteFlowchart(parallel);
        _strings.ShouldBe(new() { branch1Str, stopString });
    }

    [InlineData(false)]
    [InlineData(true)]
    [Theory]
    public void Resume_persisted_legacy_flow(bool resumeWithLegacy)
    {
        var disabledBranch = "disabledBranch";
        var activeBranch = "activeBranch";
        var stopString = "stop";
        var blockingContinuation = "blockingContinuation";
        const string blockingBookmark = "blocking";

        var root = Activities(true);
        var store = new JsonFileInstanceStore.FileInstanceStore(".\\~");
        WorkflowApplication app = new(root) { InstanceStore = store };
        app.Run();
        var appId = app.Id;
        Thread.Sleep(1000);
        app.Unload();
        root = Activities(resumeWithLegacy);
        WorkflowApplication resumedApp = new(root) { InstanceStore = store };
        ManualResetEvent manualResetEvent = new(default);
        WorkflowApplicationCompletedEventArgs completedArgs = null;
        resumedApp.Completed = args =>
        {
            completedArgs = args;
            manualResetEvent.Set();
        };
        resumedApp.Aborted = args => args.Reason.ShouldBeNull();
        resumedApp.Load(appId);
        resumedApp.Run();
        resumedApp.ResumeBookmark(blockingBookmark, null);
        manualResetEvent.WaitOne();
        completedArgs.TerminationException.ShouldBeNull();
        ((ICollection<string>)completedArgs.Outputs["Result"]).ShouldBe(new[] { activeBranch, blockingContinuation, stopString });


        ActivityWithResult<ICollection<string>> Activities(bool useLegacyFlowchart)
        {
            var blockingContinuationActivity = AddString(blockingContinuation);
            var blockingActivity = new BlockingActivity(blockingBookmark);

            var split = new FlowDecision()
            {
                True = AddString(disabledBranch).FlowTo(new FlowStep()),
                False = AddString(activeBranch)
                    .FlowTo(blockingActivity)
                    .FlowTo(blockingContinuationActivity)
                    .FlowTo(new FlowStep()),
                Condition = new LambdaValue<bool>(c => false)
            };
            split.FlowTo(AddString(stopString));
            var flowchart = new Flowchart { StartNode = split, IsLegacyFlowchart = useLegacyFlowchart };
            return new ActivityWithResult<ICollection<string>>() { In = _stringsVariable, Body = flowchart };
        }
    }

    [Fact]
    public void Should_persist_join()
    {
        var branch1Str = "branch1";
        var branch2Str = "branch2";
        var stopString = "stop";
        var blockingContStr = "blockingContinuation";
        const string blockingBookmark = "blocking";

        var root = ParallelActivities();
        var store = new JsonFileInstanceStore.FileInstanceStore(".\\~");
        WorkflowApplication app = new(root) { InstanceStore = store };
        app.Run();
        var appId = app.Id;
        Thread.Sleep(1000);
        app.Unload();
        root = ParallelActivities();
        WorkflowApplication resumedApp = new(root) { InstanceStore = store };
        ManualResetEvent manualResetEvent = new(default);
        WorkflowApplicationCompletedEventArgs completedArgs = null;
        resumedApp.Completed = args =>
        {
            completedArgs = args;
            manualResetEvent.Set();
        };
        resumedApp.Aborted = args => args.Reason.ShouldBeNull();
        resumedApp.Load(appId);
        resumedApp.Run();
        resumedApp.ResumeBookmark(blockingBookmark, null);
        manualResetEvent.WaitOne();
        completedArgs.TerminationException.ShouldBeNull();
        ((ICollection<string>)completedArgs.Outputs["Result"]).ShouldBe(new[] { branch1Str, branch2Str, blockingContStr, stopString });

        ActivityWithResult<ICollection<string>> ParallelActivities()
        {
            var blockingContinuation = AddString(blockingContStr);
            var blockingActivity = new BlockingActivity(blockingBookmark);

            var parallel = new FlowParallel().AddBranches(
                AddString(branch1Str).FlowTo(new FlowStep()),
                AddString(branch2Str).FlowTo(new FlowStep()),
                blockingActivity.FlowTo(blockingContinuation).FlowTo(new FlowStep()));
            parallel.JoinNode.FlowTo(AddString(stopString));
            var flowchart = new Flowchart { StartNode = parallel };
            return new ActivityWithResult<ICollection<string>>() { In = _stringsVariable, Body = flowchart };
        }
    }
}

public class ActivityWithResult<TResult> : NativeActivity<TResult>
{
    public Variable<TResult> In { get; set; } = new();
    public Activity Body { get; set; }
    protected override void Execute(NativeActivityContext context) => context.ScheduleActivity(Body, (context, _)=>
    {
        TResult value;
        using (context.InheritVariables())
        {
            value = In.Get(context);
        }
        context.SetValue(Result, value);
    });
}
public class BlockingActivity : NativeActivity
{
    public BlockingActivity()
    {
    }

    public BlockingActivity(string displayName)
    {
        this.DisplayName = displayName;
    }

    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        // nothing to do
    }

    protected override void Execute(NativeActivityContext context)
    {
        context.CreateBookmark(this.DisplayName, new BookmarkCallback(OnBookmarkResumed));
    }

    private void OnBookmarkResumed(NativeActivityContext context, Bookmark bookmark, object value)
    {
        // No-op
    }

    protected override bool CanInduceIdle => true;
}