﻿using System;
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
        public FirebaseClient(string baseURL)
        {
            url = baseURL;
            FirebaseStreamParser.BaseUrl = url;
        }

        public void On(string route, FirebaseEvent evt, Action<string, Dictionary<string,object>> callback)
        {
            if (!parsers.ContainsKey(route))
            {
                parsers.Add(route, new FirebaseStreamParser(route));
            }
            var parser = parsers[route];
            switch (evt)
            {
                case FirebaseEvent.Added:
                    parser.Added += callback;
                    break;
                case FirebaseEvent.Changed:
                    parser.Changed += callback;
                    break;
                case FirebaseEvent.Deleted:
                    parser.Deleted += callback;
                    break;
                default:
                    break;
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