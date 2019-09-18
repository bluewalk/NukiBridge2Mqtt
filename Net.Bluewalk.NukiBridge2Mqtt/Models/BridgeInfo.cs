using System;
using System.Collections.Generic;
using Net.Bluewalk.NukiBridge2Mqtt.Models.Enum;
using Newtonsoft.Json;

namespace Net.Bluewalk.NukiBridge2Mqtt.Models
{
    public class BridgeInfo
    {
        [JsonProperty("bridgeType")]
        public BridgeTypEnum BridgeType { get; set; }

        [JsonProperty("ids")]
        public BridgeIds BridgeIds { get; set; }

        [JsonProperty("versions")]
        public BridgeVersions BridgeVersions { get; set; }

        [JsonProperty("uptime")]
        public long UpTime { get; set; }

        [JsonProperty("currentTime")]
        public DateTimeOffset CurrentTime { get; set; }

        [JsonProperty("serverConnected")]
        public bool ServerConnected { get; set; }

        [JsonProperty("scanResults")]
        public List<ScanResult> ScanResults { get; set; }
    }
}
