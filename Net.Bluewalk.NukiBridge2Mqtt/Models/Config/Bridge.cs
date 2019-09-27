using YamlDotNet.Serialization;

namespace Net.Bluewalk.NukiBridge2Mqtt.Models.Config
{
    public class Bridge
    {
        public Callback Callback { get; set; }
        public string Url { get; set; }
        public string Token { get; set; }
        [YamlMember(Alias = "hash-token", ApplyNamingConventions = false)]
        public bool HashToken { get; set; }
        [YamlMember(Alias = "info-interval", ApplyNamingConventions =  false)]
        public int? InfoInterval { get; set; }
    }
}
