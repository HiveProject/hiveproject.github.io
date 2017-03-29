using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Text;
using FireHive.Firebase.REST.Data;

namespace FireHive.Firebase.REST
{
    internal class FirebaseStreamParser
    {
        private Thread receiveThread;
        private Thread sendThread;

        private Queue<Dictionary<string, object>> toSend = new Queue<Dictionary<string, object>>();

        HashSet<string> loadedObjects = new HashSet<string>();
        public string Route { get; private set; }
        static public string BaseUrl { get; set; }
        public event Action<string, DataBranch> Added;
        public event Action<string, DataBranch> Changed;
        public event Action<string, DataBranch> Deleted;


        //   DataBranch dataCache;
        public FirebaseStreamParser(string route) : this(route, BaseUrl)
        {
        }
        public FirebaseStreamParser(string route, string baseUrl)
        {
            toSend = new Queue<Dictionary<string, object>>();
            //  dataCache = new DataBranch();
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
            receiveThread.Name = ("FirebaseStreamParser -> " + route);
            sendThread = new Thread(() =>
            {
                var client = new System.Net.WebClient();
                while (true)
                {

                    if (toSend.Count() != 0)
                    {
                        var uri = new Uri(baseUrl + route + ".json");
                        while (toSend.Count > 0)
                        {
                            var upd = toSend.Dequeue();
                            while (toSend.Count > 0 && tryJoin(upd, toSend.Peek()))
                            { toSend.Dequeue(); }

                            //   var toUpdate = new DataBranch(upd.ToDictionary(entry => entry.Key, entry => (DataNode)new DataLeaf(entry.Value)));
                            //    dataCache.Merge(toUpdate);
                            client.UploadData(uri, "PATCH", Encoding.UTF8.GetBytes(serializeDictionary(upd)));
                        }
                    }

                    Thread.Sleep(10);

                }

            });
            sendThread.Name = ("FirebaseStreamParser <- " + route);
            sendThread.IsBackground = true;
            sendThread.Start();
        }
        private bool tryJoin(IDictionary<string, object> current, IDictionary<string, object> changes)
        {

            if (changes.Any(kvp => current.ContainsKey(kvp.Key) && current[kvp.Key] != kvp.Value))
                return false;
            //the changes dont collide. i need only to update the new keys
            foreach (var key in changes.Keys.Where(k => !current.ContainsKey(k)))
            {
                current.Add(key, changes[key]);

            }
            return true;
        }
        private string serializeDictionary(IDictionary<string, object> data)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            bool comma = false;
            foreach (var item in data)
            {
                if (comma) { sb.Append(","); } else { comma = true; }
                sb.Append("\"")
                    .Append(item.Key)
                    .Append("\":");
                if (item.Value == null)
                {
                    sb.Append("null");
                }
                else if (item.Value.GetType() == typeof(DateTime))
                {
                    sb.Append("\"")
                          .Append(((DateTime)item.Value).ToUniversalTime().ToString("s"))
                          .Append("\"");
                }
                else if (item.Value.GetType() == typeof(string))
                {
                    sb.Append("\"").Append(item.Value).Append("\"");
                }
                else { sb.Append(item.Value); }

            }

            sb.Append("}");

            return sb.ToString();
        }

        internal string Post(Dictionary<string, object> data)
        {
            var client = new System.Net.WebClient();
            string toPost = "{}";
            if (data != null)
            { toPost = Newtonsoft.Json.JsonConvert.SerializeObject(data); }
            var result = client.UploadString(new Uri(BaseUrl + Route + ".json"), "POST", toPost);
            return ((dynamic)JObject.Parse(result)).name;
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
                var result = DataNode.FromJsonString(data);
                switch (evt)
                {
                    case "put":
                        //data

                        if (result["data"] == null)
                        {
                            if (result["path"].As<string>() != "/")
                                //delete i think.
                                dataRemoved(result["path"].ToString().Substring(1), null);

                            return;
                        }
                        if (result["data"].IsLeaf)
                        {
                            //i need to check it by the path.
                            if (result["path"].As<string>() != "/")
                            {
                                var dict = new Dictionary<string, DataNode>();
                                dict[result["path"].As<string>()] =  result["data"];
                                var rootBranch = new DataBranch(dict);
                                var key = rootBranch.Keys.First();
                                dataChanged(key, rootBranch[key]);
                            }
                            else
                            {
                                dataAdded(result["path"].ToString().Substring(1), result["data"]);
                            }
                            return;
                        }

                        var dataBranch = (DataBranch)result["data"];
                        foreach (var item in dataBranch.Keys)
                        {
                            loadedObjects.Add(item);
                            dataAdded(item, dataBranch[item]);
                        }
                        break;
                    case "patch":


                        DataBranch dataNode = (DataBranch)result["data"];
                        foreach (var item in dataNode)
                        {
                            if (loadedObjects.Contains(item.Key))
                            {
                                if (item.Value == null)
                                {
                                    dataRemoved(item.Key, item.Value);
                                }
                                else
                                {
                                    dataChanged(item.Key, item.Value);
                                }
                            }
                            else
                            {
                                loadedObjects.Add(item.Key);
                                dataAdded(item.Key, item.Value);
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


            toSend.Enqueue(upd);
        }




        internal void dataAdded(string key, DataNode data)
        {
            //DataBranch dataBranch = data.AsBranch();
            //if (dataCache.ContainsKey(key))
            //{
            //    //i had something here.
            //    var realdata = dataCache[key];
            //    if (realdata == null || realdata.NotContains(dataBranch))
            //    {
            //        dataCache[key] = data;
            //        Added(key, dataBranch);
            //    }
            //}
            //else
            //{
            //    dataCache[key] = data;
            Added(key, data.AsBranch());
            // }
        }
        internal void dataChanged(string key, DataNode data)
        {
            //var realdata = dataCache[key];
            //if (realdata.NotContains(data))
            //{
            //    if (realdata.IsLeaf != data.IsLeaf || realdata.IsLeaf)
            //    {
            //        dataCache[key] = data;
            //    }
            //    else
            //    {
            //        DataBranch dataBranch = data.AsBranch();
            //        realdata.Merge(dataBranch);
            //Changed(key, realdata.AsBranch());
            Changed(key, data.AsBranch());
            //    }
            //}
        }


        internal void dataRemoved(string key, DataNode data)
        {
            if (data == null)
                Deleted(key, null);
            else
                Deleted(key, data.AsBranch());
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