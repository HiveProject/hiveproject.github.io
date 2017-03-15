using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireHive.Proxies
{
    class IEnnumeratorProxy<T> : IEnumerator<T>
    {
        private IList<T> innerList;
        private int cursor = 0;
        public IEnnumeratorProxy(IList<T> InnerList)
        {
            innerList = InnerList;
        }

        public T Current
        {
            get
            {
              return  innerList[cursor];
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return innerList[cursor];
            }
        }

        public void Dispose()
        {
            innerList = null;

        }

        public bool MoveNext()
        {
            return ++cursor < innerList.Count;
        }

        public void Reset()
        {
            cursor = 0;
        }
    }
}
