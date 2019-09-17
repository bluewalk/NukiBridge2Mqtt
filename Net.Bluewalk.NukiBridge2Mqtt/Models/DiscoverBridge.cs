using System;
using Newtonsoft.Json;

namespace Net.Bluewalk.NukiBridge2Mqtt.Models
{
    public class DiscoverBridge
    {
        [JsonProperty("bridgeId")]
        public int BridgeId { get; set; }

        [JsonProperty("ip")]
        public string Ip { get; set; }

        [JsonProperty("Port")]
        public int Port { get; set; }

        [JsonProperty("dateUpdated")]
        public DateTime DateUpdated { get; set; }
    }
}
