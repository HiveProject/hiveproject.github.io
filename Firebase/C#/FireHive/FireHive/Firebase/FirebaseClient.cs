using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FireHive.Firebase
{
    class FirebaseClient
    {
        private string url;

        StringBuilder sb = new StringBuilder();
        Dictionary<string, FirebaseStreamParser> parsers = new Dictionary<string, FirebaseStreamParser>();
        Dictionary<string, Thread> threads = new Dictionary<string, Thread>();
        public FirebaseClient(string baseURL)
        {
            url = baseURL;

        }

        public void On(string route, FirebaseEvent evt, Action<object> callback)
        {
            if (parsers.ContainsKey(route))
            { parsers[route].On(evt, callback); }
            else {
                var parser = new FirebaseStreamParser();
                var th = new Thread(() =>
                {
                    var client = new System.Net.WebClient();
                    client.Headers.Add(System.Net.HttpRequestHeader.Accept, "text/event-stream");
                    client.OpenReadCompleted += (s, e) =>
                    {
                        var sr = new StreamReader(e.Result);
                        parser.Reader = sr;
                        parser.Start();
                    };
                    client.OpenReadAsync(new Uri(url + route + ".json"));
                });
                parsers.Add(route, parser);
                threads.Add(route, th);
                th.Start();
            }
        }

        private void Client_OpenReadCompleted(object sender, System.Net.OpenReadCompletedEventArgs e)
        {

            using (var sr = new StreamReader(e.Result))
            {
                while (!sr.EndOfStream)
                {
                    sb.Append(sr.ReadLine());

                }
            }

            //   throw new NotImplementedException();
        }
    }
}
