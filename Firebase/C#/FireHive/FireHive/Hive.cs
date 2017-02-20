using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;

namespace FireHive
{
    public class Hive
    {
        //FirebaseClient database; 

        static Hive instance;
        private Roots roots;
        private AliveObjects loadedObjects;
        private Firebase.FirebaseClient client;
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
            //  database = new Firebase.Database.FirebaseClient("https://hive-1336.firebaseio.com/");
            //  roots = new Roots(database);

            client = new Firebase.FirebaseClient("https://hive-1336.firebaseio.com/");
            roots = new Roots(client);
            loadedObjects = new AliveObjects(client);
        }








        public object set(string key, object value)
        {
            return value;
        }
        public object Get(string key)
        {
            return loadedObjects.Get(roots.Get(key));
            
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
        public Dictionary<string, object> elements()
        {
            var result = new Dictionary<string, object>();
            //todo: proxyfy
            roots.ForEach((k, v) =>
            {
                result.Add(k, loadedObjects.Get(v));
            });

            return result;
        }
        public void forEach(Action<string, object> action)
        {
            foreach (var item in elements())
            {
                action(item.Key, item.Value);
            }
        }

    }
}
