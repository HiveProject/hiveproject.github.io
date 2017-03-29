using Firebase.Data;
using Firebase.Data.Changeset;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Firebase
{
    public class FirebaseClient
    {
        private string dbName;
        string version = "5";
        string firebaseUrl = "wss://s-usc1c-nss-128.firebaseio.com/.ws";//?v=5&ns=hive-1336
        ConcurrentQueue<string> sendQueue = new ConcurrentQueue<string>();
        Uri dbUri;
        System.Net.WebSockets.ClientWebSocket client;
        Thread receiveTh;
        Thread sendTh;


        Action<string> processInput;

        public FirebaseClient(Uri url)
        {
            dbUri = url;
            processInput = HandleHandshake;

            dbName = url.Authority.Substring(0, url.Authority.IndexOf("."));

            initializeConnections();

        }
        public FirebaseClient(string url) : this(new Uri(url))
        {
        }

        private void cleanupConnection()
        {
            Messages.RequestData.resetCounter();
            if (sendTh != null)
            {
                if (sendTh.ThreadState == ThreadState.Running)
                    sendTh.Abort();
                sendTh = null;
            }
            if (receiveTh != null)
            {
                if (receiveTh.ThreadState == ThreadState.Running)
                    receiveTh.Abort();
                receiveTh = null;
            }
            if (client != null)
            {
                client.Abort();
                client = null;
            }
            sendQueue = new ConcurrentQueue<string>();

        }

        public void Patch(string path, Dictionary<string, object> data)
        {
            if (!path.StartsWith("/"))
                path = "/" + path;
            Enqueue(new Messages.Request("m",
                new Messages.RequestPayload()
                {
                    Path = path,
                    Data = data
                }));
        }

        private void initializeConnections()
        {
            cleanupConnection();
            client = new System.Net.WebSockets.ClientWebSocket();
            receiveTh = new Thread(() =>
            {

                byte[] buff = new byte[2048];
                ArraySegment<byte> segment = new ArraySegment<byte>(buff);
                CancellationToken rcvToken = new CancellationToken(false);
                while (client.State == System.Net.WebSockets.WebSocketState.Open)
                {
                    var result = client.ReceiveAsync(segment, rcvToken).Result;
                    processInput(System.Text.Encoding.UTF8.GetString(buff, 0, result.Count));
                }
                Console.WriteLine("receive thread ending");
                //the socket aborted. this will be in charge of reinitializing the connection.
                initializeConnections();
            });
            receiveTh.IsBackground = true;
            receiveTh.Name = "receive";
            int keepAlive = 50 * 1000;
            sendTh = new Thread(() =>
            {
                int toSend = keepAlive;
                while (client.State == System.Net.WebSockets.WebSocketState.Open)
                {
                    while (sendQueue.Count > 0)
                    {
                        string current = string.Empty;
                        CancellationToken outToken = new CancellationToken();
                        if (sendQueue.TryDequeue(out current))
                        {
                            client.SendAsync(new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(current))
                                , System.Net.WebSockets.WebSocketMessageType.Text,
                                true, outToken);
                            Thread.Sleep(1);
                        }
                    }
                    Thread.Sleep(10);
                    toSend -= 10;
                    if (toSend <= 0)
                    {
                        sendQueue.Enqueue("0");
                        toSend = keepAlive;
                    }
                }
                Console.WriteLine("send thread ending");
            });
            sendTh.Name = "send";
            sendTh.IsBackground = true;

            processInput = HandleHandshake;
            Uri targetURI = new Uri(firebaseUrl + "?v=" + version + "&ns=" + dbName);
            CancellationToken token = new CancellationToken(false);
            client.ConnectAsync(targetURI, token).ContinueWith((t) =>
            {
                Console.WriteLine("Connected");
                receiveTh.Start();
                sendTh.Start();
            });
        }
        string authurl = "";
        string authToken = "";
        long serverTime;
        private void HandleHandshake(string data)
        {
            dynamic obj = Newtonsoft.Json.JsonConvert.DeserializeObject(data);

            obj = obj.d.d;
            serverTime = obj.ts;
            authurl = obj.h;
            authToken = obj.s;
            var initialRequest = new Messages.Request("s",
                new Messages.RequestPayload()
                {
                    initialization = new Dictionary<string, object>() { { "sdk.js.3 - 6 - 2", 1 } }
                });
            //"{\"t\":\"d\",\"d\":{\"r\":1,\"a\":\"s\",\"b\":{\"c\":{\"sdk.js.3 - 6 - 2\":1}}}}"
            Enqueue(initialRequest);
            processInput = ProcessInput;
            //tests:
            //it seems that if i request an n, it is a notification of changes, but then i have to actually do a query for the information to start coming.


            foreach (var path in subscribedPaths)
            {
                subscribeNotification(path);
            }
        }

        private void Enqueue(Messages.Request request)
        {
            sendQueue.Enqueue(Newtonsoft.Json.JsonConvert.SerializeObject(request));
        }
        private void ProcessInput(string data)
        {
            //todo: process the input to ensure message re-sending

            //Console.WriteLine(data);
            var response = Newtonsoft.Json.JsonConvert.DeserializeObject<Responses.Response>(data);
            if (response.Data.Action == null)
            {
                //reply to message.
                Console.WriteLine(string.Format("Request: {0} -> {1}", response.Data.ResquestId, response.Data.Payload.Status));
            }
            else
            {
                ChangeSet changeset = null;
                if (response.Data.Action == "d")
                {
                    //the data here comes as an object
                    JToken token = response.Data.Payload.Data as JToken;
                    if (token == null)
                    {
                        changeset = new ChangeSetLeaf(response.Data.Payload.Data);
                    }
                    else
                    {

                        changeset = ChangeSet.FromJToken(token);
                    }


                }
                else if (response.Data.Action == "m")
                {
                    //the data here comes as a dictionary of path,value
                    JToken token = response.Data.Payload.Data as JToken;


                    changeset = ChangeSet.FromFlatJToken(token);


                }
                else
                { }

                var node = localCache.Find(response.Data.Payload.Path);

                if (node == null)
                {
                    node = localCache.Find(response.Data.Payload.Path, true);
                    changeset.Type = ChangeType.Added;
                }
                node.Merge(changeset);
                handleCallbacks(response.Data.Payload.Path, changeset);
            }

        }

        private void handleCallbacks(string basePath, ChangeSet set)
        {
            handleSpecificCallbacks(basePath, set, AddedCallbacks, ChangeType.Added);
            handleSpecificCallbacks(basePath, set, ChangedCallbacks, ChangeType.Modified);
            handleSpecificCallbacks(basePath, set, RemovedCallbacks, ChangeType.Removed);
        }

        private void handleSpecificCallbacks(string basePath, ChangeSet set, Dictionary<string, List<Action<string, ChangeSet>>> callbacks, ChangeType desiredChange)
        {

            //for each callback someone asked me, i am going to filter the set to see what should i give that guy.
            foreach (var cb in callbacks.Where(kvp => kvp.Key.StartsWith(basePath) || basePath.StartsWith(kvp.Key)))
            {
                ChangeSet node;
                //those nodes to add.
                var key = cb.Key;
                if (cb.Key.Length >= basePath.Length)
                {
                    key = cb.Key.Substring(basePath.Length);
                    node = set;
                }
                else
                {
                    //the key the asked for is shorter than my base path, this means, that i have some steps missing
                    node = set;
                    Queue<string> toBuild = new Queue<string>(basePath.Split('/').Reverse());
                    while (toBuild.Count > 0)
                    {
                        var childs = new Dictionary<string, ChangeSet>();
                        childs.Add(toBuild.Dequeue(), node);
                        node = new ChangesetBranch(childs);
                    }
                }
                node = node.Find(key);

                //really not sure about this.
                foreach (var child in node.Childs.Where(t => t.Value.Type == desiredChange))
                {
                    foreach (var action in cb.Value)
                    {
                        action(child.Key, child.Value);
                    }
                }

            }


        }


        DataBranch localCache = new DataBranch();

        Dictionary<string, List<Action<string, ChangeSet>>> AddedCallbacks = new Dictionary<string, List<Action<string, ChangeSet>>>();
        Dictionary<string, List<Action<string, ChangeSet>>> ChangedCallbacks = new Dictionary<string, List<Action<string, ChangeSet>>>();
        Dictionary<string, List<Action<string, ChangeSet>>> RemovedCallbacks = new Dictionary<string, List<Action<string, ChangeSet>>>();
        List<string> subscribedPaths = new List<string>();
        private void subscribeNotification(string path)
        {
            var req = new Messages.Request("n",
                      new Messages.RequestPayload() { Path = "/" + path });
            Enqueue(req);
            req = new Messages.Request("q",
                new Messages.RequestPayload() { Path = "/" + path, h = string.Empty });
            Enqueue(req);
        }





        public void On(string path, SubscribeOperations operation, Action<string, ChangeSet> action)
        {
            Dictionary<string, List<Action<string, ChangeSet>>> callbacks = null;
            switch (operation)
            {
                case SubscribeOperations.Added:
                    callbacks = AddedCallbacks;
                    break;
                case SubscribeOperations.Changed:
                    callbacks = ChangedCallbacks;
                    break;
                case SubscribeOperations.Removed:
                    callbacks = RemovedCallbacks;
                    break;
            }
            if (!callbacks.ContainsKey(path))
            {
                callbacks[path] = new List<Action<string, ChangeSet>>();

            }
            if (!subscribedPaths.Contains(path))
            { subscribeNotification(path); }
            callbacks[path].Add(action);
        }






        public string Post(string Route, Dictionary<string, object> data = null)
        {
            var client = new System.Net.WebClient();
            string toPost = "{}";
            if (data != null)
            { toPost = Newtonsoft.Json.JsonConvert.SerializeObject(data); }
            var result = client.UploadString(new Uri(dbUri.AbsoluteUri + Route + ".json"), "POST", toPost);
            return ((dynamic)JObject.Parse(result)).name;
        }
    }
}
