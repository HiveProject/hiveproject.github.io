using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Firebase.Data.Changeset;

namespace Firebase.Data
{
    internal class DataLeaf : DataNode
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
            var myPath = Path;
            var result = new DataBranch(new Dictionary<string, DataNode>() { { "value", this } });
            result.Path = myPath;
            Path = myPath;
            return result;
        }

        public override bool NotContains(DataNode data)
        {
            if (!data.IsLeaf) return true;
            //todo: here i might check for that different int type kind of problem.
            return !((DataLeaf)data).Value.Equals(value);
        }


        public override bool ContainsKey(string key)
        {
            return false;
        }

        public override DataNode Find(string path,bool create=false)
        {
            if (Path == path)
                return this;
            throw new NotImplementedException();
        }

        internal override void Merge(ChangeSet data)
        {

            //if it is a leaf, i can take its value, otherwise it should have been handled by my parent

            if (data.IsLeaf)
            {
                var leaf = (ChangeSetLeaf)data;

                if (this.value==leaf.Value)
                {
                    leaf.Type = ChangeType.None;
                }
                else
                {
                    value = leaf.Value;
                    leaf.Type = ChangeType.Modified;
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
