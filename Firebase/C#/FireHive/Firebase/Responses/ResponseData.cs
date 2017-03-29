using Newtonsoft.Json;
using System;

namespace Firebase.Responses
{
    public class ResponseData
    {
        [JsonProperty(PropertyName = "r", NullValueHandling = NullValueHandling.Ignore)]
        public UInt16 ResquestId { get; set; }
        [JsonProperty(PropertyName = "a", NullValueHandling = NullValueHandling.Ignore)]
        public string Action { get; set; }

        [JsonProperty(PropertyName = "b", NullValueHandling = NullValueHandling.Ignore)]
        public ResponsePayload Payload { get; set; }




    }
}