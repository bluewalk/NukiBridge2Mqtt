using RestSharp.Serializers;
using System.Collections.Generic;

namespace Net.Bluewalk.NukiBridge2Mqtt.Models
{
    public class CallbackList
    {
        [SerializeAs(Name = "callbacks")]
        public List<Callback> Callbacks { get; set; }
    }
}
