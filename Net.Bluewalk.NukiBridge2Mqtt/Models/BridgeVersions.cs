using RestSharp.Serializers;

namespace Net.Bluewalk.NukiBridge2Mqtt.Models
{
    public class BridgeVersions
    {
        [SerializeAs(Name = "firmwareVersion")]
        public string FirmwareVersion { get; set; }

        [SerializeAs(Name = "wifiFirmwareVersion")]
        public string WifiFirmwareVersion { get; set; }
    }
}