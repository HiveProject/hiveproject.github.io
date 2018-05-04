using System;
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

            for (int i = 0; i < amount; i++)
            {
                var s = await c.CreateNewSession();
                result.Add(s);
                await s.Navigate("http://hiveproject.github.io/Firebase/");
            }

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
        public static async Task hiveRemove(this ChromeSession s, string key) 
        {
            await s.EvalObject(string.Format(hiveRemoveString, key));
        }
    }
}
