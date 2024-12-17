// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;

namespace System.Activities.Hosting;

[DataContract]
[Fx.Tag.XamlVisible(false)]
public sealed class BookmarkInfo
{
    private string _bookmarkName;
    private BookmarkScopeInfo _scopeInfo;
    private string _ownerDisplayName;
    private Activity _owner;

    internal BookmarkInfo() { }
    internal BookmarkInfo(string bookmarkName, string ownerDisplayName, BookmarkScopeInfo scopeInfo, Activity owner)
    {
        BookmarkName = bookmarkName;
        OwnerDisplayName = ownerDisplayName;
        ScopeInfo = scopeInfo;
        _owner = owner;
    }

    public string BookmarkName
    {
        get => _bookmarkName;
        private set => _bookmarkName = value;
    }

    public string OwnerDisplayName
    {
        get => _ownerDisplayName;
        private set => _ownerDisplayName = value;
    }

    public BookmarkScopeInfo ScopeInfo
    {
        get => _scopeInfo;
        private set => _scopeInfo = value;
    }

    public Activity Owner 
    { 
        get => _owner;
        set => _owner = value;
    }

    [DataMember(Name = "BookmarkName")]
    internal string SerializedBookmarkName
    {
        get => BookmarkName;
        set => BookmarkName = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "OwnerDisplayName")]
    internal string SerializedOwnerDisplayName
    {
        get => OwnerDisplayName;
        set => OwnerDisplayName = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "ScopeInfo")]
    internal BookmarkScopeInfo SerializedScopeInfo
    {
        get => ScopeInfo;
        set => ScopeInfo = value;
    }
}
