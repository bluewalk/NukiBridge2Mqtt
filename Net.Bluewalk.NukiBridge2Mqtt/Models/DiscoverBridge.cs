using System;
using System.Collections.Generic;
using RestSharp.Serializers;

namespace Net.Bluewalk.NukiBridge2Mqtt.Models
{
    public class DiscoverBridge
    {
        [SerializeAs(Name = "bridgeId")]
        public int BridgeId { get; set; }

        [SerializeAs(Name = "ip")]
        public string Ip { get; set; }

        [SerializeAs(Name = "Port")]
        public int Port { get; set; }

        [SerializeAs(Name = "dateUpdated")]
        public DateTime DateUpdated { get; set; }
    }
}
