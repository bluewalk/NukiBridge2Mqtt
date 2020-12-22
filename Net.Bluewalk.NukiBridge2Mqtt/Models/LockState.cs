using System;
using Net.Bluewalk.NukiBridge2Mqtt.Models.Enum;
using Newtonsoft.Json;

namespace Net.Bluewalk.NukiBridge2Mqtt.Models
{
    public class LockState
    {
        [JsonProperty("state")]
        public StateEnum State { get; set; }

        [JsonProperty("stateName")]
        public string StateName { get; set; }

        [JsonProperty("batteryCritical")]
        public bool BatteryCritical { get; set; }

        [JsonProperty("keypadBatteryCritical")]
        public bool KeypadBatteryCritical { get; set; }

        [JsonProperty("doorsensorState")]
        public DoorSensorStateEnum DoorSensorState { get; set; }

        [JsonProperty("doorsensorStateName")]
        public string DoorSensorStateName { get; set; }
        
        [JsonProperty("ringactionTimestamp")]
        public DateTimeOffset RingActionTimestamp { get; set; }

        [JsonProperty("ringactionState")]
        public bool RingActionState { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }
    }
}
