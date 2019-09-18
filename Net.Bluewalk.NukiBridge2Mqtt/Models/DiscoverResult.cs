using System.Collections.Generic;
using Newtonsoft.Json;

namespace Net.Bluewalk.NukiBridge2Mqtt.Models
{
    public class DiscoverResult
    {
        [JsonProperty("bridges")]
        public List<DiscoverBridge> Bridges { get; set; }

        [JsonProperty("errorCode")]
        public int ErrorCode { get; set; }
    }
}
