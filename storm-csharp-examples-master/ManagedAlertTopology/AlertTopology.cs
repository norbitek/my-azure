using System.Collections.Generic;
using Microsoft.SCP;
using Microsoft.SCP.Topology;
using System.Configuration;


namespace ManagedAlertTopology
{
    [Active(true)]
    public class AlertTopology : TopologyDescriptor
    {
        public ITopologyBuilder GetTopologyBuilder()
        {
            TopologyBuilder topologyBuilder = new TopologyBuilder("AlertTopology");

            var eventHubPartitions = int.Parse(ConfigurationManager.AppSettings["EventHubPartitions"]);

            topologyBuilder.SetEventHubSpout(
                "EventHubSpout",
                new EventHubSpoutConfig(
                    ConfigurationManager.AppSettings["EventHubSharedAccessKeyName"],
                    ConfigurationManager.AppSettings["EventHubSharedAccessKey"],
                    ConfigurationManager.AppSettings["EventHubNamespace"],
                    ConfigurationManager.AppSettings["EventHubEntityPath"],
                    eventHubPartitions),
                eventHubPartitions);

            // Set a customized JSON Serializer to serialize a Java object (emitted by Java Spout) into JSON string
            // Here, full name of the Java JSON Serializer class is required
            List<string> javaSerializerInfo = new List<string>() { "microsoft.scp.storm.multilang.CustomizedInteropJSONSerializer" };

            var boltConfig = new StormConfig();

            topologyBuilder.SetBolt(
                typeof(ParserBolt).Name,
                ParserBolt.Get,
                new Dictionary<string, List<string>>()
                {
                    {Constants.DEFAULT_STREAM_ID, new List<string>(){ "temp", "createDate", "deviceId" } }
                },
                eventHubPartitions,
                enableAck: true
                ).
                DeclareCustomizedJavaSerializer(javaSerializerInfo).
                shuffleGrouping("EventHubSpout").
                addConfigurations(boltConfig);

            topologyBuilder.SetBolt(
                typeof(EmitAlertBolt).Name,
                EmitAlertBolt.Get,
                new Dictionary<string, List<string>>()
                {
                    {Constants.DEFAULT_STREAM_ID, new List<string>(){ "reason", "temp", "createDate", "deviceId" } }
                },
                eventHubPartitions,
                enableAck: true
                ).
                shuffleGrouping(typeof(ParserBolt).Name).
                addConfigurations(boltConfig);

            var topologyConfig = new StormConfig();
            topologyConfig.setMaxSpoutPending(8192);
            topologyConfig.setNumWorkers(eventHubPartitions);

            topologyBuilder.SetTopologyConfig(topologyConfig);
            return topologyBuilder;
        }
    }
}
