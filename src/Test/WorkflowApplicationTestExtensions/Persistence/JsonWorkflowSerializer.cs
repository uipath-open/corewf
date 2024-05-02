﻿using System.IO;
using System.Activities.Runtime.DurableInstancing;
using System.Collections.Generic;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace WorkflowApplicationTestExtensions.Persistence;


using InstanceDictionary = Dictionary<string, InstanceValue>;
using XInstanceDictionary = IDictionary<XName, InstanceValue>;

public class JsonWorkflowSerializer : IWorkflowSerializer
{
    XInstanceDictionary IWorkflowSerializer.LoadWorkflowInstance(Stream sourceStream)
    {
        JsonTextReader reader = new(new StreamReader(sourceStream));
        var workflowState = Serializer().Deserialize(reader, typeof(InstanceDictionary));
        return WorkflowSerializerHelpers.ToNameDictionary(workflowState);
    }
    void IWorkflowSerializer.SaveWorkflowInstance(XInstanceDictionary workflowInstanceState, Stream destinationStream)
    {
        JsonTextWriter writer = new(new StreamWriter(destinationStream));
        Serializer().Serialize(writer, workflowInstanceState.ToSave());
        writer.Flush();
    }
    private static JsonSerializer Serializer() => new()
    {
        Formatting = Formatting.Indented,
        TypeNameHandling = TypeNameHandling.Auto,
        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
        ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
        ObjectCreationHandling = ObjectCreationHandling.Replace
    };

}
