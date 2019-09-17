using Net.Bluewalk.NukiBridge2Mqtt.Models.Enum;
using RestSharp.Serializers;

namespace Net.Bluewalk.NukiBridge2Mqtt.Models
{
    public class ScanResult
    {
        [SerializeAs(Name = "nukiId")]
        public long NukiId { get; set; }

        [SerializeAs(Name = "deviceType")]
        public DeviceTypeEnum DeviceType { get; set; }

        [SerializeAs(Name = "name")]
        public string Name { get; set; }

        [SerializeAs(Name = "rssi")]
        public long Rssi { get; set; }

        [SerializeAs(Name = "paired")]
        public bool Paired { get; set; }
    }
}