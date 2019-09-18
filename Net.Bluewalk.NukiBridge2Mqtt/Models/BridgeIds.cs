using Newtonsoft.Json;

namespace Net.Bluewalk.NukiBridge2Mqtt.Models
{
    public class BridgeIds
    {
        [JsonProperty("hardwareId")]
        public long HardwareId { get; set; }

        [JsonProperty("serverId")]
        public long ServerId { get; set; }
    }
}