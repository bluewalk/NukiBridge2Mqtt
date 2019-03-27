using RestSharp.Serializers;

namespace Net.Bluewalk.NukiBridge2Mqtt.Models
{
    public class Lock
    {
        [SerializeAs(Name = "nukiId")]
        public long NukiId { get; set; }

        [SerializeAs(Name = "name")]
        public string Name { get; set; }

        [SerializeAs(Name = "lastKnownState")]
        public LastKnownState LastKnownState { get; set; }
    }
}
