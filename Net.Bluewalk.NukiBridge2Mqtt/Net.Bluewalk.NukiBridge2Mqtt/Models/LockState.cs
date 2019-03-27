using RestSharp.Serializers;

namespace Net.Bluewalk.NukiBridge2Mqtt.Models
{
    public class LockState
    {
        [SerializeAs(Name = "state")]
        public LockStateEnum StateEnum { get; set; }

        [SerializeAs(Name = "stateName")]
        public string StateName { get; set; }

        [SerializeAs(Name = "batteryCritical")]
        public bool BatteryCritical { get; set; }

        [SerializeAs(Name = "success")]
        public bool Success { get; set; }
    }
}
