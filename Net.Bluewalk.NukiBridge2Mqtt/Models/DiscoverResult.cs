using System.Collections.Generic;
using RestSharp.Serializers;

namespace Net.Bluewalk.NukiBridge2Mqtt.Models
{
    public class DiscoverResult
    {
        [SerializeAs(Name ="bridges")]
        public List<DiscoverBridge> Bridges { get; set; }

        [SerializeAs(Name = "errorCode")]
        public int ErrorCode { get; set; }
    }
}
