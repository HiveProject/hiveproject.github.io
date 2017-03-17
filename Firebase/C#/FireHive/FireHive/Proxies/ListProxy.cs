using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireHive.Proxies
{
    public class ListProxy : IList<object>
    {
        private List<object> innerList;
        private Action<object, string> setExecuted;

        public ListProxy(List<object> InnerList, Action<object, string> SetExecuted)
        {
            innerList = InnerList;
            setExecuted = SetExecuted;
        }
        public object this[int index]
        {
            get
            {
                //need to step through a proxy
                return Hive.Current.getProxy(innerList[index]);
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public int Count
        {
            get
            {
                //this does not have anything interesting
                return innerList.Count;

            }
        }

        public bool IsReadOnly
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void Add(object item)
        {
            //i need to ensure that this object is expandible and whatsoever.
            //i need to remove proxies..
            item = Hive.Current.UnProxyfy(item);
            innerList.Add( Hive.Current.loadedObjects.Get( Hive.Current.loadedObjects.Add(item)));
            setExecuted(innerList, (innerList.Count - 1).ToString());
            
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(object item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(object[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<object> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public int IndexOf(object item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, object item)
        {
            throw new NotImplementedException();
        }

        public bool Remove(object item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return innerList.GetEnumerator(); 
        }
    }
}
