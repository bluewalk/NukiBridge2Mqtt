using RestSharp.Serializers;

namespace Net.Bluewalk.NukiBridge2Mqtt.Models
{
    public class RequestResult
    {
        [SerializeAs(Name = "success")]
        public bool Success { get; set; }

        [SerializeAs(Name = "message")]
        public string Message { get; set; }
    }
}
