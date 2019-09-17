using Net.Bluewalk.NukiBridge2Mqtt.Models.Enum;
using RestSharp.Deserializers;

namespace Net.Bluewalk.NukiBridge2Mqtt.Models
{
    public class CallbackBody
    {
        [DeserializeAs(Name = "nukiId")]
        public int nukiId { get; set; }

        [DeserializeAs(Name = "deviceType")]
        public DeviceTypeEnum DeviceType { get; set; }

        [DeserializeAs(Name = "mode")]
        public LockModeEnum Mode { get; set; }

        [DeserializeAs(Name = "mode")]
        public LockActionOpenerEnum ModeOpener { get; set; }

        [DeserializeAs(Name = "state")]
        public int state { get; set; }

        [DeserializeAs(Name = "stateName")]
        public string stateName { get; set; }

        [DeserializeAs(Name = "batteryCritical")]
        public bool batteryCritical { get; set; }
    }
}
