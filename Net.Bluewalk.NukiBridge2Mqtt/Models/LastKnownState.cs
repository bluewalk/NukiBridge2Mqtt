using System;
using Net.Bluewalk.NukiBridge2Mqtt.Models.Enum;
using Newtonsoft.Json;

namespace Net.Bluewalk.NukiBridge2Mqtt.Models
{
    public class LastKnownState
    {
        [JsonProperty("mode")]
        public ModeEnum Mode { get; set; }

        [JsonProperty("state")]
        public StateEnum State { get; set; }

        [JsonProperty("stateName")]
        public string StateName { get; set; }

        [JsonProperty("batteryCritical")]
        public bool BatteryCritical { get; set; }

        [JsonProperty("doorsensorState")]
        public DoorSensorStateEnum DoorSensorState { get; set; }

        [JsonProperty("doorsensorStateName")]
        public string DoorSensorStateName { get; set; }

        [JsonProperty("timestamp")]
        public DateTimeOffset Timestamp { get; set; }
    }
}