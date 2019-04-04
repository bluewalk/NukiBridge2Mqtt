using System.Text.RegularExpressions;
using RestSharp.Serializers;

namespace Net.Bluewalk.NukiBridge2Mqtt.Models
{
    public class Lock
    {
        [SerializeAs(Name = "nukiId")]
        public int NukiId { get; set; }

        [SerializeAs(Name = "name")]
        public string Name { get; set; }

        [SerializeAs(Name = "lastKnownState")]
        public LastKnownState LastKnownState { get; set; }

        public string NameMqtt => Regex.Replace(Name, "[^a-zA-Z0-9]+", "-").ToLower();
    }
}
