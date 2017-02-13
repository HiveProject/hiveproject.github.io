﻿using System;
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
        private WebClient client;
        Thread th;

        public string Route { get; private set; }
        static public string BaseUrl { get; set; }
        public event Action<string,Dictionary<string, object>> Added;
        public event Action<string, Dictionary<string, object>> Changed;
        public event Action<string, Dictionary<string, object>> Deleted;
        public FirebaseStreamParser(string route) : this(route, BaseUrl)
        {
        }
        public FirebaseStreamParser(string route, string baseUrl)
        {
            Added += (k,e) => { };
            Changed += (k, e) => { };
            Deleted += (k,e) => { };

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
                var result = (Dictionary<string,object>)mapJson(data); 
                switch (evt)
                {
                    case "put":
                        //data
                        var d = (Dictionary<string,object>)(result["data"]);
                        foreach (var item in d.Keys)
                        {

                            Added(item,result[item] as Dictionary<string,object>);

                        }
                        break;
                    default:
                        break;
                }
            }
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
                    return token.Select(mapJsonToken).ToList();

                default:
                    return ((JValue)token).Value;
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