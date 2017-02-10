using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireHive
{
    abstract class Map<TKey, TValue>
    {
        protected Dictionary<TKey, TValue> innerDictionary;
        public Map()
        { innerDictionary = new Dictionary<TKey, TValue>(); }

        public virtual TValue Get(TKey key)
        {
            if (key == null) return default(TValue);
            if (!innerDictionary.ContainsKey(key))
                return default(TValue);
            return innerDictionary[key];
        }
        public virtual bool Set(TKey key, TValue value)
        {
            innerDictionary[key] = value;
            return true;
        }
        public virtual TKey GetKey(TValue value)
        {
            foreach (var item in innerDictionary)
            {
                if (item.Value.Equals(value))
                { return item.Key; }
            }
            return default(TKey);
        }
        public virtual void Clear()
        {
            innerDictionary.Clear();
        }
        public virtual bool Delete(TKey key)
        {
            return innerDictionary.Remove(key);
        }
        public virtual bool Has(TKey key)
        {
            return innerDictionary.ContainsKey(key);
        }
        public virtual IEnumerable<TKey> Keys()
        {
            return innerDictionary.Keys;
        }
        public virtual IEnumerable<TValue> Values()
        {
            return innerDictionary.Values;
        }
        public virtual void ForEach(Action<TKey, TValue> action)
        {
            foreach (var item in innerDictionary)
            {
                action(item.Key, item.Value);
            }
        }
    }
}
