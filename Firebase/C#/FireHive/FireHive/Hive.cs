using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;
using FireHive.Proxies;
using FireHive.Dynamic;
using Firebase;

namespace FireHive
{
    public class Hive
    {
        //FirebaseClient database; 


        public event EventHandler<EventArgs> Initialized;
        static Hive instance;
        internal Roots roots;
        internal AliveObjects loadedObjects;
        private FirebaseClient client;


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

            client = new FirebaseClient("https://hive-1336.firebaseio.com/");
            roots = new Roots(client);
            loadedObjects = new AliveObjects(client);
            loadedObjects.Initialized += (s,e)=>Initialized?.Invoke(this, EventArgs.Empty);
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

        }

        internal object UnProxyfy(object item)
        {
            if (item.GetType().IsValueType) return item;
            if (Current.proxies.ContainsValue(item))
            {
                var result =  Current.proxies.FirstOrDefault(kvp => kvp.Value == item).Key;
                if (result == null) return item;
                return result;
            }
            return item;
        }

        private dynamic createProxy(object obj)
        {
            var t = obj.GetType();
            if (t == typeof(ExpandibleObject))
            { return new ExpandoObjectProxy((ExpandibleObject)obj, SetExecuted); }
            //todo, other proxies
            if (t == typeof(List<object>))
                return new ListProxy((List<object>)obj, SetExecuted);
            return obj;
        }


        public dynamic set(string key, object value)
        {
            string innerKey = loadedObjects.Add(value);
            roots.Set(key, innerKey);
            return getProxy(loadedObjects.Get(innerKey));
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
