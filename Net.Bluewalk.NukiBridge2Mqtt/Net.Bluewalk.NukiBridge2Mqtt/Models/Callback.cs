using System;
using RestSharp.Serializers;

namespace Net.Bluewalk.NukiBridge2Mqtt.Models
{
    public class Callback
    {
        [SerializeAs(Name = "id")]
        public long Id { get; set; }

        [SerializeAs(Name = "url")]
        public Uri Url { get; set; }
    }
}