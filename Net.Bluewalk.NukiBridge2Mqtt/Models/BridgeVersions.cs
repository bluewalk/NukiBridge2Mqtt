using Newtonsoft.Json;

namespace Net.Bluewalk.NukiBridge2Mqtt.Models
{
    public class BridgeVersions
    {
        [JsonProperty("firmwareVersion")]
        public string FirmwareVersion { get; set; }

        [JsonProperty("wifiFirmwareVersion")]
        public string WifiFirmwareVersion { get; set; }

        [JsonProperty("appVersion")]
        public string AppVersion { get; set; }
    }
}