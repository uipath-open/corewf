// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;

[Serializable]
public class ExtensionRequiredException : Exception
{
    private const string RequiredExtensionTypeName = "requiredExtensionType";

    public string RequiredExtensionTypeFullName => Data[RequiredExtensionTypeName] as string;

    public ExtensionRequiredException(Type requiredType)
        : base()
    {
        Data.Add(RequiredExtensionTypeName, requiredType.FullName);
    }

    public ExtensionRequiredException(Type requiredType, string message)
        : base(message)
    {
        Data.Add(RequiredExtensionTypeName, requiredType.FullName);
    }

    public ExtensionRequiredException(Type requiredType, string message, Exception innerException)
        : base(message, innerException)
    {
        Data.Add(RequiredExtensionTypeName, requiredType.FullName);
    }

    public ExtensionRequiredException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
