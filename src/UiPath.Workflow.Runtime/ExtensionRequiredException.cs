// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;

[Serializable]
public class ExtensionRequiredException : Exception
{
    private const string RequiredExtensionTypeName = "requiredExtensionType";

    public string RequiredExtensionTypeFullName { get; }

    public ExtensionRequiredException(Type requiredType)
        : base()
    {
        RequiredExtensionTypeFullName = requiredType.FullName;
    }

    public ExtensionRequiredException(Type requiredType, string message)
        : base(message)
    {
        RequiredExtensionTypeFullName = requiredType.FullName;
    }

    public ExtensionRequiredException(Type requiredType, string message, Exception innerException)
        : base(message, innerException)
    {
        RequiredExtensionTypeFullName = requiredType.FullName;
    }

    public ExtensionRequiredException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        RequiredExtensionTypeFullName = info.GetString(RequiredExtensionTypeName);
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(RequiredExtensionTypeName, RequiredExtensionTypeFullName, typeof(string));
    }
}
