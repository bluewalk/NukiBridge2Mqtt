using RestSharp.Serializers;

namespace Net.Bluewalk.NukiBridge2Mqtt.Models
{
    public class LockActionResult : RequestResult
    {
        [SerializeAs(Name = "batteryCritical")]
        public bool BatteryCritical { get; set; }
    }
}
