// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using Shouldly;
using System.Activities;
using System.Activities.Statements;
using System.Text;
using Xunit;

namespace TestCases.Runtime
{
    public class MissingRequiredExtensionTests
    {
        [Fact]
        public void RunningActivityWithUnregisteredRequiredExtensionShouldThrow()
        {
            var sequence = new Sequence
            {
                Activities = { new MissingRequiredExtension<StringBuilder>() }
            };
            WorkflowApplication instance = new WorkflowApplication(sequence);

            var ex = Assert.Throws<ExtensionRequiredException>(instance.Run);
            ex.RequiredExtensionType.ShouldBe(typeof(StringBuilder));
        }

        private class MissingRequiredExtension<T> : NativeActivity
        {
            protected override void CacheMetadata(NativeActivityMetadata metadata)
            {
                metadata.RequireExtension(typeof(T));
            }

            protected override void Execute(NativeActivityContext context)
            {
            }
        }
    }
}
