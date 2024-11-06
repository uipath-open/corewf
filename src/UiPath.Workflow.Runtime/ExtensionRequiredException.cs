// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;

[Serializable]
public class ExtensionRequiredException : Exception
{
    public Type RequiredExtensionType { get; }

    public ExtensionRequiredException(Type requiredType)
        : base()
    {
        RequiredExtensionType = requiredType;
    }

    public ExtensionRequiredException(Type requiredType, string message)
        : base(message)
    {
        RequiredExtensionType = requiredType;
    }

    public ExtensionRequiredException(Type requiredType, string message, Exception innerException)
        : base(message, innerException)
    {
        RequiredExtensionType = requiredType;
    }

    public ExtensionRequiredException(Type requiredType, SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        RequiredExtensionType = requiredType;
    }
}
