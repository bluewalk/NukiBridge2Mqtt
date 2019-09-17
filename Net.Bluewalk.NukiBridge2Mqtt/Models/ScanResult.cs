using Net.Bluewalk.NukiBridge2Mqtt.Models.Enum;
using Newtonsoft.Json;

namespace Net.Bluewalk.NukiBridge2Mqtt.Models
{
    public class ScanResult
    {
        [JsonProperty("nukiId")]
        public long NukiId { get; set; }

        [JsonProperty("deviceType")]
        public DeviceTypeEnum DeviceType { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("rssi")]
        public long Rssi { get; set; }

        [JsonProperty("paired")]
        public bool Paired { get; set; }
    }
}