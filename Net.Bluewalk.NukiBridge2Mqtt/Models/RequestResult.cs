using Newtonsoft.Json;

namespace Net.Bluewalk.NukiBridge2Mqtt.Models
{
    public class RequestResult
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
