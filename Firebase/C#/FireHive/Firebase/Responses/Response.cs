using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Firebase.Responses
{
    class Response
    {
        [JsonProperty(PropertyName = "t")]
        public string DataField { get; set; }
        [JsonProperty(PropertyName = "d")]
        public ResponseData Data { get; set; }
    }
}
