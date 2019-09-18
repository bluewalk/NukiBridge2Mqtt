using Newtonsoft.Json;

namespace Net.Bluewalk.NukiBridge2Mqtt.Models
{
    public class LockActionResult : RequestResult
    {
        [JsonProperty("batteryCritical")]
        public bool BatteryCritical { get; set; }
    }
}
