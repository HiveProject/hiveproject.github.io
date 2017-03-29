using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Firebase.Messages
{

    class RequestData
    {
        public static void resetCounter() {
            count = 0;
        }
        static UInt16 count;
        public RequestData()
        {
            Id = ++count;
        }

        public RequestData(string action,RequestPayload payload):this()
        {
            Action = action;
            Payload = payload;
        }

        [JsonProperty(PropertyName = "r")]
        public UInt16 Id { get; set; }

        [JsonProperty(PropertyName = "a")]
        public string Action { get; set; }

        [JsonProperty(PropertyName = "b")]
        public RequestPayload Payload { get; set; }
    }
}
