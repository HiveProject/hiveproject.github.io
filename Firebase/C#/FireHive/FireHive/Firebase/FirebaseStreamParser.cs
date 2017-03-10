using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace FireHive.Firebase
{
    internal class FirebaseStreamParser
    {
        private Thread receiveThread;
        private Thread sendThread;

        Queue<Tuple<outMessage, Dictionary<string, object>>> ToSend = new Queue<Tuple<outMessage, Dictionary<string, object>>>();
        HashSet<string> loadedObjects = new HashSet<string>();
        public string Route { get; private set; }
        static public string BaseUrl { get; set; }
        public event Action<string, Dictionary<string, object>> Added;
        public event Action<string, Dictionary<string, object>> Changed;
        public event Action<string, Dictionary<string, object>> Deleted;



        Dictionary<string, object> dataCache;
        public FirebaseStreamParser(string route) : this(route, BaseUrl)
        {
        }
        public FirebaseStreamParser(string route, string baseUrl)
        {
            dataCache = new Dictionary<string, object>();
            BaseUrl = baseUrl;
            Added += (k, e) => { };
            Changed += (k, e) => { };
            Deleted += (k, e) => { };

            Route = route;
            receiveThread = new Thread(() =>
            {
                var client = new System.Net.WebClient();
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
            receiveThread.IsBackground = true;
            receiveThread.Start();

            sendThread = new Thread(() =>
            {
                var client = new System.Net.WebClient();
                while (true)
                {
                    while (ToSend.Count > 0)
                    {
                        var msg = ToSend.Dequeue();

                        switch (msg.Item1)
                        {
                            case outMessage.PATCH:
                                using (StreamWriter sw = new StreamWriter(client.OpenWrite(new Uri(baseUrl + route + ".json"), "PATCH")))
                                {
                                    sw.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(msg.Item2));
                                }
                                break;
                            default:
                                //do nothing
                                break;
                        }
                    }
                    System.Threading.Thread.Sleep(10);
                }
            });
            sendThread.IsBackground = true;
            sendThread.Start();
        }

        internal string Post(Dictionary<string, object> data)
        {
            var client = new System.Net.WebClient();
            string toPost = "{}";
            if (data != null)
            { toPost = Newtonsoft.Json.JsonConvert.SerializeObject(data); }
            var result = client.UploadString(new Uri(BaseUrl + Route + ".json"), "POST", toPost);
            return mapJson(result).asDictionary()["name"].ToString();
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
                    evt = parts[1].Trim();
                    if (evt == "keep-alive")
                    { evt = null; }
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
                var result = (Dictionary<string, object>)mapJson(data);
                switch (evt)
                {
                    case "put":
                        //data

                        if (result["data"] == null)
                        {
                            if (result["path"] as string != "/")
                                //delete i think.
                                dataRemoved(result["path"].ToString().Substring(1), null);

                            return;
                        }
                        if (result["data"] is string)
                        {
                            dataAdded(result["path"].ToString().Substring(1), new Dictionary<string, object>() { { "value", result["data"] } });
                            return;
                        }


                        var d = (Dictionary<string, object>)(result["data"]);
                        foreach (var item in d.Keys)
                        {
                            loadedObjects.Add(item);
                            if (d[item] is string)
                            {
                                dataAdded(item, new Dictionary<string, object>() { { "value", d[item] } });
                            }
                            else
                            {
                                dataAdded(item, d[item] as Dictionary<string, object>);
                            }

                        }
                        break;
                    case "patch":


                        d = (Dictionary<string, object>)(result["data"]);
                        Dictionary<string, object> toUpdate = new Dictionary<string, object>();
                        foreach (var item in d.Keys)
                        {
                            Queue<string> path = new Queue<string>(item.Split('/'));
                            Dictionary<string, object> current = toUpdate;
                            while (path.Count > 1)
                            {
                                string k = path.Dequeue();
                                Dictionary<string, object> next = null;
                                if (!current.ContainsKey(k))
                                {
                                    next = new Dictionary<string, object>();
                                    current[k] = next;
                                }
                                current = current[k].asDictionary();
                            }
                            current[path.Dequeue()] = d[item];

                        }
                        foreach (var item in toUpdate)
                        {
                            //todo: change this to use the internal data.
                            if (loadedObjects.Contains(item.Key))
                            {
                                if (item.Value == null)
                                {
                                    dataRemoved(item.Key, item.Value as Dictionary<string, object>);
                                }
                                else
                                {
                                    if (item.Value.GetType() == typeof(string))
                                    {

                                        dataChanged(item.Key, new Dictionary<string, object>() { { "value", item.Value } });
                                    }
                                    else
                                    {
                                        dataChanged(item.Key, item.Value as Dictionary<string, object>);
                                    }
                                }
                            }
                            else
                            {
                                loadedObjects.Add(item.Key);
                                if (item.Value.GetType() == typeof(string))
                                {
                                    dataAdded(item.Key, new Dictionary<string, object>() { { "value", item.Value } });
                                }
                                else
                                {
                                    dataAdded(item.Key, item.Value as Dictionary<string, object>);
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        internal void Patch(Dictionary<string, object> upd)
        {
            ToSend.Enqueue(new Tuple<outMessage, Dictionary<string, object>>(outMessage.PATCH, upd));
        }

        private object mapJson(string input)
        {
            return mapJsonToken(JToken.Parse(input));

        }

        private object mapJsonToken(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    return token.Children<JProperty>()
                                .ToDictionary(prop => prop.Name,
                                              prop => mapJsonToken(prop.Value));

                case JTokenType.Array:
                    var i = 0;
                    return token.Select(mapJsonToken).ToDictionary(p => (i++).ToString(), p => p);

                default:
                    return ((JValue)token).Value;
            }
        }

        internal void dataAdded(string key, Dictionary<string, object> data)
        {
            dataCache[key] = data;
            Added(key, data); 
        }
        internal void dataChanged(string key, Dictionary<string, object> data)
        {
            var realdata = dataCache[key].asDictionary();
            mergeDictionaries(data, realdata);
            Changed(key, realdata);
        }


        internal void dataRemoved(string key, Dictionary<string, object> data)
        {
            Deleted(key, data);
        }

        private void mergeDictionaries(Dictionary<string, object> from, Dictionary<string, object> to)
        {
            foreach (var item in from)
            {
                if (to.ContainsKey(item.Key))
                {
                    var d1 = item.Value.asDictionary();
                    var d2 = to[item.Key].asDictionary();
                    if (d1 != null)
                    {
                        if (d2 != null)
                        {
                            mergeDictionaries(d1, d2);
                        }
                        else
                        {
                            to[item.Key] = d1;
                        }
                    }
                    else {
                        //not a dictionary
                        to[item.Key] = item.Value;
                    }
                }
                else
                { }

            }
        }
        bool isPrimitive(object obj)
        {
            if (obj == null)
            { return false; }
            //todo: numbers and stuff have different names here. i should map them.
            return isPrimitiveTypeName(obj.GetType().Name);
        }
        bool isPrimitiveTypeName(string name)
        {
            return name == "Number" ||
                name == "Date" ||
                name == "Boolean" ||
                name == "String";
        }

    }
}