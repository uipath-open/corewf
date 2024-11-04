// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;

public class ExtensionRequiredException : ValidationException
{
    public Type RequiredExtensionType { get; private set; }

    public ExtensionRequiredException(Type requiredType)
        : base(SR.RequiredExtensionTypeNotFound(requiredType.ToString()))
    {
        RequiredExtensionType = requiredType;
    }
}
