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
            if (value.GetType() == typeof(int))
                value = Convert.ToInt64(value);
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

        public override DataBranch AsBranch()
        {
            return new DataBranch(new Dictionary<string, DataNode>() { { "value", this } });
        }

        public override bool NotContains(DataNode data)
        {
            if (!data.IsLeaf) return true;
            //todo: here i might check for that different int type kind of problem.
            return !((DataLeaf)data).Value.Equals(value);
        }

        public override void Merge(DataBranch data)
        {
            throw new NotImplementedException();
        }

        public override bool ContainsKey(string key)
        {
            return false;
        }
    }
}
