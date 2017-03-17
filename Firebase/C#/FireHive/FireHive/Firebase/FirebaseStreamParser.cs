using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Linq;
using FireHive.Firebase.Data;

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
        public event Action<string, DataBranch> Added;
        public event Action<string, DataBranch> Changed;
        public event Action<string, DataBranch> Deleted;



        DataBranch dataCache;
        public FirebaseStreamParser(string route) : this(route, BaseUrl)
        {
        }
        public FirebaseStreamParser(string route, string baseUrl)
        {
            dataCache = new DataBranch(new Dictionary<string, DataNode>());
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
                                //todo: merge using datanodes
                                //  mergeDictionaries(splitPath(msg.Item2), dataCache);
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
            //return mapJson(result).asDictionary()["name"].ToString();
            return "";
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

                            dataAdded(result["path"].ToString().Substring(1), result["data"]);
                            return;
                        }

                        var dataBranch = (DataBranch)result["data"];
                        foreach (var item in dataBranch.Keys)
                        {
                            loadedObjects.Add(item);
                            if (dataBranch[item].IsLeaf)
                            {

                                dataAdded(item, dataBranch[item]);
                            }
                            else
                            {
                                dataAdded(item, dataBranch[item]);
                            }

                        }
                        break;
                    case "patch":


                        DataBranch dataNode = (DataBranch)result["data"];
                        foreach (var item in dataNode)
                        {
                            //todo: change this to use the internal data.
                            if (loadedObjects.Contains(item.Key))
                            {
                                if (item.Value == null)
                                {
                                    dataRemoved(item.Key, item.Value);
                                }
                                else
                                {
                                    if (item.Value.IsLeaf)
                                    {

                                        dataChanged(item.Key, item.Value);
                                    }
                                    else
                                    {
                                        dataChanged(item.Key, item.Value);
                                    }
                                }
                            }
                            else
                            {
                                loadedObjects.Add(item.Key);
                                if (item.Value.IsLeaf)
                                {
                                    dataAdded(item.Key, item.Value);
                                }
                                else
                                {
                                    dataAdded(item.Key, item.Value);
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




        internal void dataAdded(string key, DataNode data)
        {
            if (dataCache.ContainsKey(key))
            {
                //i had something here.
                var realdata = dataCache[key];
                if (realdata == null || realdata.Differs(data))
                {
                    dataCache[key] = data;
                    Added(key, data.AsBranch());
                }
            }
            else
            {
                dataCache[key] = data;
                Added(key, data.AsBranch());
            }
        }
        internal void dataChanged(string key, DataNode data)
        {
            var realdata = dataCache[key];
            if (realdata.Differs(data))
            {
                realdata.Merge(data);
                Changed(key, realdata.AsBranch());
            }
        }


        internal void dataRemoved(string key, DataNode data)
        {
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