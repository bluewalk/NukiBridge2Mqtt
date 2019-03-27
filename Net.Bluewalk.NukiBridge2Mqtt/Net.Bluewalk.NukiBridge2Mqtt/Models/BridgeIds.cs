using RestSharp.Serializers;

namespace Net.Bluewalk.NukiBridge2Mqtt.Models
{
    public class BridgeIds
    {
        [SerializeAs(Name = "hardwareId")]
        public long HardwareId { get; set; }

        [SerializeAs(Name = "serverId")]
        public long ServerId { get; set; }
    }
}