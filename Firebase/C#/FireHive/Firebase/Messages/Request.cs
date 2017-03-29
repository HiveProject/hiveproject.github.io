using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Firebase.Messages
{
    class Request
    {
        public Request(string action,RequestPayload payload)
        {
            DataField = "d";
            Data = new RequestData(action,payload);
        }

        [JsonProperty(PropertyName = "t")]
        public string DataField { get; set; }
        [JsonProperty(PropertyName = "d")]
        public RequestData Data { get; set; }
    }
}
