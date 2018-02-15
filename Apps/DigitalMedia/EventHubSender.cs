using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System.Configuration;
using HomeOS.Hub.Common;
using System.Runtime.Serialization;

namespace HomeOS.Hub.Apps.DigitalMedia
{
    public class EventHubSender
    {
        private Microsoft.ServiceBus.Messaging.EventHubSender senderClient = null;
        public EventHubSender()
        {
            string connectionString = ConfigurationManager.AppSettings["DigitalMedia.EventHub.ConnectionString"];
            senderClient = Microsoft.ServiceBus.Messaging.EventHubSender.CreateFromConnectionString(connectionString);
        }


        public void SendEvents(string eventType, int slot, int joint, bool value, string message){

            // Create the device/temperature metric
            CrestronEvent info = new CrestronEvent()
            {
                EventType = eventType, Slot = slot, Joint = joint, 
                Value = value, Message = message};
            var serializedString = JsonConvert.SerializeObject(info);
            EventData data = new EventData(Encoding.UTF8.GetBytes(serializedString))
            {
                PartitionKey = info.Joint.ToString()
            };

            // Set measure time as user property
            data.Properties.Add("EventTime", DateTime.Now.ToLongTimeString());
            data.Properties.Add("TimeZone", TimeZone.CurrentTimeZone.StandardName);

            senderClient.Send(data);
        }

    }

    [DataContract]
    public class CrestronEvent
    {
        [DataMember]
        public string EventType { get; set; }
        [DataMember]
        public int Slot { get; set; }
        
        [DataMember]
        public int Joint { get; set; }

        [DataMember]
        public bool Value { get; set; }
        [DataMember]
        public string Message { get; set; }
    }
}
