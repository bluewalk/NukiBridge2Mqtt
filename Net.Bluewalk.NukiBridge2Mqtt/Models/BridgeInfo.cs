using RestSharp.Serializers;
using System;
using System.Collections.Generic;
using Net.Bluewalk.NukiBridge2Mqtt.Models.Enum;

namespace Net.Bluewalk.NukiBridge2Mqtt.Models
{
    public class BridgeInfo
    {
        [SerializeAs(Name = "bridgeType")]
        public BridgeTypEnum BridgeType { get; set; }

        [SerializeAs(Name = "ids")]
        public BridgeIds BridgeIds { get; set; }

        [SerializeAs(Name = "versions")]
        public BridgeVersions BridgeVersions { get; set; }

        [SerializeAs(Name = "uptime")]
        public long UpTime { get; set; }

        [SerializeAs(Name = "currentTime")]
        public DateTimeOffset CurrentTime { get; set; }

        [SerializeAs(Name = "serverConnected")]
        public bool ServerConnected { get; set; }

        [SerializeAs(Name = "scanResults")]
        public List<ScanResult> ScanResults { get; set; }
    }
}
