using Shouldly;
using System;
using System.Activities;
using System.Activities.Hosting;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace WorkflowApplicationTestExtensions
{
    public class WorkflowApplicationTestSamples
    {
        [Fact]
        public void RunUntilCompletion_Outputs()
        {
            var app = new WorkflowApplication(new DynamicActivity
            {
                Properties = { new DynamicActivityProperty { Name = "result", Type = typeof(OutArgument<string>) } },
                Implementation = () => new Assign<string> { To = new Reference<string>("result"), Value = "value" }
            });
            app.RunUntilCompletion().Outputs["result"].ShouldBe("value");
        }

        [Fact]
        public void RunUntilCompletion_Faulted()
        {
            var app = new WorkflowApplication(new Throw { Exception = new InArgument<Exception>(_ => new ArgumentException()) });
            Should.Throw<ArgumentException>(() => app.RunUntilCompletion());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void RunUntilCompletion_AutomaticPersistence(bool useJsonSerialization)
        {
            var app = new WorkflowApplication(new SuspendingWrapper
            {
                Activities =
                {
                    new WriteLine(),
                    new NoPersistAsyncActivity(),
                    new WriteLine()
                }
            });
            var result = app.RunUntilCompletion(useJsonSerialization: useJsonSerialization);
            result.PersistenceCount.ShouldBe(4);
        }

        [Fact]
        public void SuspensionLeadsToBookmarkCreation()
        {
            var suspendingActivity = new SuspendingWrapper
            {
                Activities =
                {
                    new WriteLine(),
                }
            };
            var app = new WorkflowApplication(suspendingActivity);
            var result = app.RunUntilCompletion();
            var bookmarkFromUnload = result.UnloadedBookmarks.Single();
            var bookmarkFromPersistIdle = result.PersistIdle.Single();

            bookmarkFromPersistIdle.Owner.ShouldBe(suspendingActivity);
            bookmarkFromPersistIdle.BookmarkName.ShouldBe(bookmarkFromPersistIdle.BookmarkName);
        }

        [Fact]
        public async Task DelaySuspensionLeadsToBookmarkCreation()
        {
            var suspendingActivity = new Sequence()
            {
                Activities =
                {
                    new Delay
                    {
                        Duration = new InArgument<TimeSpan>(TimeSpan.FromSeconds(1)),
                    },
                        new WriteLine()
                        {
                            Text = new InArgument<string>("Delay was successful.")
                        }

                }
            };
            var app = new WorkflowApplication(suspendingActivity);
            var result = await Task.Run( () =>app.RunUntilCompletion());
            var bookmarkFromUnload = result.UnloadedBookmarks.Single();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ShouldPersistFaultContextInCatchBlock(bool useJsonSerialization)
        {
            var app = new WorkflowApplication(
                new SuspendingWrapper
                {
                    Activities =
                    {
                        new TryCatch
                        {
                            Try = new Throw
                            {
                                Exception = new InArgument<Exception>(
                                    activityContext => new ArgumentException("CustomArgumentException")
                                )
                            },
                            Catches =
                            {
                                new Catch<ArgumentException> {
                                    Action = new ActivityAction<ArgumentException>
                                    {
                                        Argument = new DelegateInArgument<ArgumentException>
                                        {
                                            Name = "exception"
                                        },
                                        Handler = new SuspendingWrapper
                                        {
                                            Activities =
                                            {
                                                new WriteLine(),
                                                new NoPersistAsyncActivity(),
                                                new WriteLine()
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            );

            var result = app.RunUntilCompletion(useJsonSerialization: useJsonSerialization);
            result.PersistenceCount.ShouldBe(6);
        }
    }
}
