using Newtonsoft.Json;

namespace Firebase.Responses
{
    public class ResponsePayload
    {
        [JsonProperty(PropertyName = "s", NullValueHandling = NullValueHandling.Ignore)]
        public string Status { get; set; }
        [JsonProperty(PropertyName = "p", NullValueHandling = NullValueHandling.Ignore)]
        public string Path { get; set; }
        [JsonProperty(PropertyName = "d", NullValueHandling = NullValueHandling.Ignore)]
        public object Data{ get; set; }
    }
}