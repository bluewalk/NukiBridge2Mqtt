using System.Text.RegularExpressions;
using Net.Bluewalk.NukiBridge2Mqtt.Models.Enum;
using Newtonsoft.Json;

namespace Net.Bluewalk.NukiBridge2Mqtt.Models
{
    public class Device
    {
        [JsonProperty("nukiId")]
        public int NukiId { get; set; }

        [JsonProperty("deviceType")]
        public DeviceTypeEnum DeviceType { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("lastKnownState")]
        public LastKnownState LastKnownState { get; set; }

        public string NameMqtt => Regex.Replace(Name, "[^a-zA-Z0-9]+", "-").ToLower();
    }
}
