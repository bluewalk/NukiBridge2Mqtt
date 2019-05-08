using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Net.Bluewalk.NukiBridge2Mqtt.Models.Config
{
    public class Callback
    {
        public IPAddress Address { get; set; }
        public int? Port { get; set; }
    }
}
