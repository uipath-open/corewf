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
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void RunActivityWithRequiredExtension(bool registerExtension)
        {
            var sequence = new Sequence
            {
                Activities = { new ActivityWithRequiredExtension(registerExtension) }
            };

            var exception = Record.Exception(new WorkflowApplication(sequence).Run);

            if (registerExtension)
            {
                exception.ShouldBeNull();
            }
            else
            {
                exception.ShouldBeOfType<ExtensionRequiredException>();
                ((ExtensionRequiredException)exception).RequiredExtensionType.ShouldBe(typeof(StringBuilder));
            }
        }

        private class ActivityWithRequiredExtension : NativeActivity
        {
            private readonly bool _registerExtension;

            public ActivityWithRequiredExtension(bool registerExtension)
            {
                _registerExtension = registerExtension;
            }

            protected override void CacheMetadata(NativeActivityMetadata metadata)
            {
                metadata.RequireExtension<StringBuilder>();
                if (_registerExtension)
                {
                    metadata.AddDefaultExtensionProvider(() => new StringBuilder());
                }
            }

            protected override void Execute(NativeActivityContext context)
            {
            }
        }
    }
}
