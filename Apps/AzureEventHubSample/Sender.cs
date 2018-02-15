using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeOS.Hub.Apps.AzureEventHubSample
{
    using Microsoft.ServiceBus.Messaging;
   
    using Newtonsoft.Json;

    public class Sender
    {
       
        string eventHubName;
      
        public Sender(string eventHubName)
        {
            this.eventHubName = eventHubName;
        }

        public bool SendEvents(string homeHubId, DateTime dt, string sensorName, string sensorRole, string sensorData)
        {
            // Create EventHubClient
            EventHubClient client = EventHubClient.Create(this.eventHubName);

            bool bEventSent = false;
 
			
			try
			{
			    List<Task> tasks = new List<Task>();
			    // Send messages to Event Hub
			    Console.WriteLine("Sending messages to Event Hub {0}", client.Path);
			   

                // Create the device/temperature metric
                MetricEvent info = new MetricEvent() { HomeHubId = homeHubId, SensorName = sensorName,  SensorData = sensorData, SensorRole = sensorRole, 
                 EntryDateTime = dt};
                var serializedString = JsonConvert.SerializeObject(info);
                EventData data = new EventData(Encoding.UTF8.GetBytes(serializedString))
                {
                    PartitionKey = info.HomeHubId.ToString()
                };

                // Set user properties if needed
                data.Properties.Add("Type", "Telemetry_" + DateTime.Now.ToLongTimeString());
                OutputMessageInfo(DateTime.Now.ToString() + " SENDING: ", data, info);

                // Send the metric to Event Hub
                tasks.Add(client.SendAsync(data));
			 

			    Task.WaitAll(tasks.ToArray());
                bEventSent = true;
 			}
			catch (Exception exp)
			{
			    Console.WriteLine("Error on send: " + exp.Message);
               
			}

            client.CloseAsync().Wait();
            return bEventSent;
        }

        static void OutputMessageInfo(string action, EventData data, MetricEvent info)
        {
            if (data == null)
            {
                return;
            }
            if (info != null)
            {
                Console.WriteLine("{0} - HomeHubId: {1}, SensorName: {2}, SensorData: {3}, SensorRole: {4}.", action, info.HomeHubId, info.SensorName, info.SensorData, info.SensorRole);
            }
        }
    }
}
