using System.Collections.Generic;
using Newtonsoft.Json;

namespace Net.Bluewalk.NukiBridge2Mqtt.Models
{
    public class CallbackList
    {
        [JsonProperty("callbacks")]
        public List<Callback> Callbacks { get; set; }
    }
}
