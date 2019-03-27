using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestSharp.Serializers;

namespace Net.Bluewalk.NukiBridge2Mqtt.Models
{
    public class CallbackBody
    {
        [SerializeAs(Name = "nukiId")]
        public long NukiId { get; set; }

        [SerializeAs(Name = "state")]
        public LockStateEnum State { get; set; }

        [SerializeAs(Name = "stateName")]
        public string StateName { get; set; }

        [SerializeAs(Name = "batteryCritical")]
        public bool BatteryCritical { get; set; }
    }
}
