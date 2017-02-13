using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;

namespace FireHive.Firebase
{
    internal class FirebaseStreamParser
    {
        private WebClient client;
        Thread th;

        public string Route { get; private set; }
        static public string BaseUrl { get; set; }
        public event Action<Dictionary<string, object>> Added;
        public event Action<Dictionary<string, object>> Changed;
        public event Action<Dictionary<string, object>> Deleted;
        public FirebaseStreamParser(string route) : this(route, BaseUrl)
        {
        }
        public FirebaseStreamParser(string route, string baseUrl)
        {
            Added += (e) => { };
            Changed += (e) => { };
            Deleted += (e) => { };

            Route = route;
            th = new Thread(() =>
            {
                client = new System.Net.WebClient();
                client.Headers.Add(System.Net.HttpRequestHeader.Accept, "text/event-stream");
                client.OpenReadCompleted += (s, e) =>
                {
                    using (StreamReader sr = new StreamReader(e.Result))
                    {
                        while (!sr.EndOfStream)
                        {
                            parse(sr.ReadLine());
                        }
                    }
                };
                client.OpenReadAsync(new Uri(baseUrl + route + ".json"));
            });
            th.Start();
        }

        string evt = null;
        internal void parse(string data)
        {
            if (evt == null)
            {
                string[] parts = data.Split(':');
                if (parts.Length != 2 || parts[0] != "event")
                { //garbage
                    return;
                }
                else
                {
                    evt = parts[1];
                    Console.WriteLine(string.Format("received {0}", evt));
                    return;

                }
            }
            else
            {
                //here i'll need to parse the data, i'm going to get a new textstream for it.
                //this might be an oportunity for improvement.

                if (!data.StartsWith("data:"))
                {
                    evt = null;
                    return;//garbage
                }
                //todo, some events do not have any data.
                data = data.Substring(5);
                dynamic temp = Newtonsoft.Json.JsonConvert.DeserializeObject(data);
                var result = mapJson(temp.data);
                switch (evt)
                {
                    case "put":
                        Added(result);
                        break;
                    default:
                        break;
                }
            }
        }

        private Dictionary<string, object> mapJson(dynamic obj)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();            
            foreach (dynamic item in obj)
            {
                result[item.Name] = mapJson(item);
            }
            return result;
        }


    }
}