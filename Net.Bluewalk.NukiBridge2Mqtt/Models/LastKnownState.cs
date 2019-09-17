using System;
using Net.Bluewalk.NukiBridge2Mqtt.Models.Enum;
using RestSharp.Serializers;

namespace Net.Bluewalk.NukiBridge2Mqtt.Models
{
    public class LastKnownState
    {
        [SerializeAs(Name = "state")]
        public LockStateEnum State { get; set; }

        [SerializeAs(Name = "stateName")]
        public string StateName { get; set; }

        [SerializeAs(Name = "batteryCritical")]
        public bool BatteryCritical { get; set; }

        [SerializeAs(Name = "timestamp")]
        public DateTimeOffset Timestamp { get; set; }
    }
}