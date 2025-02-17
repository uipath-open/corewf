using Shouldly;
using System;
using System.Activities;
using System.Activities.Statements;
using WorkflowApplicationTestExtensions;
using Xunit;
using WorkflowApplicationTestExtensions.Persistence;
using System.Threading;
using Nito.AsyncEx.Interop;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;

namespace TestCases.Runtime;

public class ExecutingActivityTrackerTests
{
    protected virtual IWorkflowSerializer Serializer => new DataContractWorkflowSerializer();
    Tracker Tracker = new Tracker();
    [Fact]
    public void TestTracking()
    {
        Run(new SomeRootActivity());
        Tracker.AllCurrents.ShouldBe(
        [
            "[Native] SomeRootActivity <= Execute",
            "[Native] someCanceled <= Execute",
            "[AsyncCode] AsyncCoded <= BeginExecute",
            "[Code] Coded <= Execute",
            "[Native] SomeRootActivity <= OnCompletionCoded",
            "[AsyncCode] AsyncCoded <= EndExecute",
            "[Native] SomeRootActivity <= OnCompletionCoded",
            "[Native] SomeRootActivity <= OnBookmark",
            "[Native] Sequence",
            "[Native] SomeRootActivity <= OnCompletion",
            "[Code] Throw",
            "[Code] LambdaValue`1",
            "[Code] Throw",
            "[NativeFault] SomeRootActivity <= OnFault",
            "[Native] SomeRootActivity <= OnThrowCompletion",
            "[Native] someCanceled <= Cancel",
            "[Native] SomeRootActivity <= OnSuspendedCompletion"
        ]);
    }

    public class Coded : CodeActivity
    {
        protected override void Execute(CodeActivityContext context)
        {
            CheckTracker(context, this);
        }
    }
    public class AsyncCoded : AsyncCodeActivity
    {
        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            CheckTracker(context, this);
            
            return ApmAsyncFactory.ToBegin(Task.CompletedTask, callback, state);

        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            CheckTracker(context, this);
            ApmAsyncFactory.ToEnd(result);
        }
    }
    public class someCanceled : NativeActivity
    {
        protected override bool CanInduceIdle => true;
        protected override void Execute(NativeActivityContext context)
        {
            CheckTracker(context, this);
            context.CreateBookmark("some", OnBookmark);
        }

        private void OnBookmark(NativeActivityContext context, Bookmark bookmark, object value)
        {
            throw new NotImplementedException();
        }

        protected override void Cancel(NativeActivityContext context)
        {
            CheckTracker(context, this);
            base.Cancel(context);
        }
    }
    public class SomeRootActivity : NativeActivity
    {
        Activity _some = new Sequence();
        Activity _throw = new Throw() { Exception = new InArgument<Exception>(_ => new ArgumentException("throw")) };
        Activity _suspended = new someCanceled();
        Activity _coded = new Coded();
        Activity _asyncCoded = new AsyncCoded();
        protected override bool CanInduceIdle => true;
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);
            metadata.SetChildrenCollection([_some, _throw, _suspended, _coded, _asyncCoded]);
        }
        protected override void Execute(NativeActivityContext context)
        {
            CheckTracker(context, this);
            context.CreateBookmark($"{WorkflowApplicationTestExtensions.WorkflowApplicationTestExtensions.AutoResumedBookmarkNamePrefix}{Guid.NewGuid()}", OnBookmark);
            context.ScheduleActivity(_coded, OnCompletionCoded);
            context.ScheduleActivity(_asyncCoded, OnCompletionCoded);
            context.ScheduleActivity(_suspended, OnSuspendedCompletion);
        }

        private void OnCompletionCoded(NativeActivityContext context, ActivityInstance completedInstance)
        {
            CheckTracker(context, this);
        }

        private void OnSuspendedCompletion(NativeActivityContext context, ActivityInstance completedInstance)
        {
            CheckTracker(context, this);
        }

        private void OnFault(NativeActivityFaultContext faultContext, Exception propagatedException, ActivityInstance propagatedFrom)
        {
            CheckTracker(faultContext, this);
            faultContext.HandleFault();
        }

        private void OnBookmark(NativeActivityContext context, Bookmark bookmark, object value)
        {
            CheckTracker(context, this);
            context.ScheduleActivity(_some, OnCompletion);
        }

        private void OnCompletion(NativeActivityContext context, ActivityInstance completedInstance)
        {
            CheckTracker(context, this);
            context.ScheduleActivity(_throw, OnThrowCompletion, OnFault);
        }

        private void OnThrowCompletion(NativeActivityContext context, ActivityInstance completedInstance)
        {
            CheckTracker(context, this);
            context.CancelChildren();
        }
    }
    private void Run(Activity activity)
    {
        var app = new WorkflowApplication(activity)
        {
            InstanceStore = new MemoryInstanceStore(Serializer),
        };
        Action<WorkflowApplication> onResume = a => a.Extensions.Add<IExecutingActivityTracker>(() => Tracker);
        onResume(app);
        app.RunUntilCompletion(onResume);
    }

    private static void CheckTracker(ActivityContext ctx, Activity that = null, [CallerMemberName] string caller = null)
    {
        if (that is not null)
            that.ShouldBe(ctx.Activity);
        var tracker = (Tracker)ctx.GetExtension<IExecutingActivityTracker>();
        tracker.CurrentActivity.ShouldBe(ctx.Activity);
        var lastIndex = tracker.AllCurrents.Count() - 1;
        tracker.AllCurrents[lastIndex] += " <= " + caller;
    }
}
public class Tracker : IExecutingActivityTracker
{
    public Activity CurrentActivity => _currentActivity.Value;

    public List<string> AllCurrents = [];

    private readonly AsyncLocal<Activity> _currentActivity = new();
    public void OnActivityContextReinitialized(ActivityContext activityContext)
    {
        _currentActivity.Value = activityContext.Activity;
        AllCurrents.Add($"[{activityContext.GetType().Name.Replace("Activity","").Replace("Context", "")}] {activityContext.Activity.GetType().Name}");
    }
}