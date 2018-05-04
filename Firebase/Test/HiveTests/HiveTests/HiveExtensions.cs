﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Tera.ChromeDevTools;

namespace HiveTests
{
    public static class HiveExtensions
    {
        private const string hiveSetString = "hive.set(\"{0}\",{1})";
        private const string hiveGetString = "hive.get(\"{0}\")";
        private const string hiveRemoveString = "hive.remove(\"{0}\")";

        public static async Task<List<ChromeSession>> CreateHiveSessions(this Chrome c, int amount)
        {
            List<ChromeSession> result = new List<ChromeSession>();
            int finished = 0;
            for (int i = 0; i < amount; i++)
            {
                var s = await c.CreateNewSession();
                result.Add(s);
                s.PageLoaded += (cs, e) => { finished++; };
                await s.Navigate("http://hiveproject.github.io/Firebase/");
            }
            while (finished < amount)
                await Task.Delay(10);

            await Task.Delay(100);
            return result;
        }

        public static async Task<T> hiveSetValue<T>(this ChromeSession s, string key, T value) where T : struct
        {
            return await s.EvalValue<T>(string.Format(hiveSetString, key, value));
        }
        public static async Task<T> hiveGetValue<T>(this ChromeSession s, string key) where T : struct
        {
            return await s.EvalValue<T>(string.Format(hiveGetString, key));
        }


        public static async Task<T> hiveWaitUntilGetValue<T>(this ChromeSession s, string key,int timeout=1000) where T : struct
        {
            while (timeout>0)
            {
                try
                {
                    //TODO(tera): use the json parser to print the correct format of the values.
                    var t = await s.EvalValue<T>(string.Format(hiveGetString, key));
                    return t;
                }
                catch (Exception ex)
                {
                    //is this correct?
                }
                await Task.Delay(100);
            }
            throw new TimeoutException("The desired response was not ready.");
        }


        public static async Task hiveRemove(this ChromeSession s, string key)
        {
            await s.EvalObject(string.Format(hiveRemoveString, key));
        }


    }
}
