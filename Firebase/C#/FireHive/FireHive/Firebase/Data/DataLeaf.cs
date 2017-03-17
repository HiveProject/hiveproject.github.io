using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireHive.Firebase.Data
{
    class DataLeaf : DataNode
    {
        private object value;

        public DataLeaf(object value)
        {
            this.value = value;
        }

        public override DataNode this[string key]
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsLeaf
        {
            get
            {
                return true;
            }
        }
        public object Value { get { return value; } set { this.value = value; } }
        public override T As<T>()
        {
            if (!(value is T))
                return default(T);
            return (T)value;
        }
    }
}
