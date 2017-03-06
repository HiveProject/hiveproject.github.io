using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;
using FireHive.Proxies;

namespace FireHive
{
    public class Hive
    {
        //FirebaseClient database; 

        static Hive instance;
        private Roots roots;
        private AliveObjects loadedObjects;
        private Firebase.FirebaseClient client;


        internal Dictionary<object, dynamic> proxies;
        public static Hive Current
        {
            get
            {
                if (instance == null) instance = new Hive();
                return instance;
            }
        }
        private Hive()
        {
            proxies = new Dictionary<object, dynamic>();

            client = new Firebase.FirebaseClient("https://hive-1336.firebaseio.com/");
            roots = new Roots(client);
            loadedObjects = new AliveObjects(client);
        }



        internal dynamic getProxy(object obj)
        {
            if (obj == null) return null;
            if (!proxies.ContainsKey(obj))
                proxies.Add(obj, createProxy(obj));
            return proxies[obj];
        }
        private void SetExecuted(object rcvr, string name)
        {

            loadedObjects.UpdateField(rcvr, name);
            //if value is a proxy, i need the real thing.
         
        }
        private dynamic createProxy(object obj)
        {
            var t = obj.GetType();
            if (t == typeof(ExpandoObject))
            { return new ExpandoObjectProxy(obj,SetExecuted); }
            //todo, other proxies
            return obj; 
        }


        public dynamic set(string key, object value)
        {
            return getProxy(value);
        }
        public dynamic Get(string key)
        {
            return getProxy(loadedObjects.Get(roots.Get(key)));
            
        }
        public void remove(string key)
        {
            loadedObjects.Delete(roots.Get(key));
            roots.Delete(key);
        }
        public void removeElement(object value)
        {
            var k = loadedObjects.GetKey(value);
            var rk = roots.GetKey(k);
            remove(rk);

        }
        public IEnumerable<string> keys()
        {
            return roots.Keys();
        }
        public Dictionary<string, dynamic> elements()
        {
            var result = new Dictionary<string, dynamic>();
            //todo: proxyfy
            roots.ForEach((k, v) =>
            {
                result.Add(k, getProxy(loadedObjects.Get(v)));
            });

            return result;
        }
        public void forEach(Action<string, dynamic> action)
        {
            foreach (var item in elements())
            {
                action(item.Key, item.Value);
            }
        }

    }
}
