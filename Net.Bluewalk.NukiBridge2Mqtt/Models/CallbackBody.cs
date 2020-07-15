using Net.Bluewalk.NukiBridge2Mqtt.Models.Enum;
using Newtonsoft.Json;

namespace Net.Bluewalk.NukiBridge2Mqtt.Models
{
    public class CallbackBody
    {
        [JsonProperty("nukiId")]
        public int NukiId { get; set; }

        [JsonProperty("deviceType")]
        public DeviceTypeEnum DeviceType { get; set; }

        [JsonProperty("mode")]
        public ModeEnum Mode { get; set; }
        
        [JsonProperty("state")]
        public int State { get; set; }

        [JsonProperty("stateName")]
        public string StateName { get; set; }

        [JsonProperty("batteryCritical")]
        public bool BatteryCritical { get; set; }

        [JsonProperty("doorsensorState")]
        public DoorSensorStateEnum DoorSensorState { get; set; }

        [JsonProperty("doorsensorStateName")]
        public string DoorSensorStateName { get; set; }
    }
}
