using YamlDotNet.Serialization;

namespace Net.Bluewalk.NukiBridge2Mqtt.Models.Config
{
    public class Mqtt
    {
        public string Host { get; set; }
        public int? Port { get; set; }
        [YamlMember(Alias = "root-topic", ApplyNamingConventions = false)]
        public string RootTopic { get; set; }
    }
}
