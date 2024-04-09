using System.Activities.Validation;
using System.Dynamic;
using System.Linq;
// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Statements;

public abstract class FlowNode
{
    private Flowchart _owner;
    private int _cacheId;

    internal FlowNode()
    {
        Index = -1;
    }

    internal int Index { get; set; }

    internal bool IsOpen => _owner != null;

    internal Flowchart Flowchart => _owner;
    protected NativeActivityMetadata Metadata { get; private set; }

    // Returns true if this is the first time we've visited this node during this pass
    internal bool Open(Flowchart owner, NativeActivityMetadata metadata)
    {
        if (_cacheId == owner.CacheId)
        {
            // We've already visited this node during this pass
            if (!ReferenceEquals(_owner, owner))
            {
                metadata.AddValidationError(SR.FlowNodeCannotBeShared(_owner.DisplayName, owner.DisplayName));
            }

            // Whether we found an issue or not we don't want to change
            // the metadata during this pass.
            return false;
        }

        // if owner.ValidateUnconnectedNodes - Flowchart will be responsible for calling OnOpen for all the Nodes (connected and unconnected)
        if (!owner.ValidateUnconnectedNodes)
        {
            OnOpen(owner, metadata);
        }
        _owner = owner;
        _cacheId = owner.CacheId;
        Index = -1;

        return true;
    }

    internal virtual void OnOpen(Flowchart owner, NativeActivityMetadata metadata)
    { }

    internal virtual IEnumerable<Activity> GetChildActivities()
        => null;
    

    internal abstract IReadOnlyList<FlowNode> GetSuccessors();

    internal void EndCacheMetadata(NativeActivityMetadata metadata) 
    {
        Metadata = metadata;
        OnEndCacheMetadata();
    }

    protected virtual void OnEndCacheMetadata() { }

    internal virtual Flowchart.NodeInstance CreateInstance()
        => new();
    internal virtual void Execute() { }

    protected virtual void OnCompletionCallback() { }

    internal virtual void OnCompletionCallback<T>(T result)
    {
        switch (result)
        {
            case null:
                OnCompletionCallback();
                break;
            case bool b:
                OnCompletionCallback(b);
                break;
            default:
                throw new NotSupportedException();
        }
    }

    protected virtual void OnCompletionCallback(bool result) { }

    protected void AddValidationError(string message, IEnumerable<FlowNode> nodes = null)
    {
        Metadata.AddValidationError(new ValidationError(message)
        {
            SourceDetail = (nodes ?? Array.Empty<FlowNode>()).Concat(new[] { this }).ToArray()
        });
    }

    public override string ToString() => GetType().Name;
}
