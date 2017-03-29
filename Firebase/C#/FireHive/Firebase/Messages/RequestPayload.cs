using Newtonsoft.Json;
using System.Collections.Generic;

namespace Firebase.Messages
{
    public class RequestPayload
    {
        [JsonProperty(PropertyName = "p", NullValueHandling = NullValueHandling.Ignore)]
        public string Path { get; set; }



        [JsonProperty(PropertyName = "c", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string,object> initialization { get; set; }

        [JsonProperty(PropertyName = "h",NullValueHandling =NullValueHandling.Ignore)]
        public string h { get; set; }

        [JsonProperty(PropertyName = "d", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string,object> Data { get; set; }

    }
}