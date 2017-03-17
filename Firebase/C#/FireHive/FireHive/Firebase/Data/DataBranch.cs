using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireHive.Firebase.Data
{
    class DataBranch : DataNode, IEnumerable<KeyValuePair<string, DataNode>>
    {
        private Dictionary<string, DataNode> children;
        public DataBranch()
        { children = new Dictionary<string, DataNode>(); }
        public DataBranch(Dictionary<string, DataNode> childs)
        {
            children = new Dictionary<string, DataNode>();
            foreach (var item in childs)
            {
                addPathedValue(item.Key, item.Value);
            }
        }

        private void addPathedValue(string path, DataNode value)
        {
            if (path.StartsWith("/"))
                path = path.Substring(1);
            if (path.Contains('/'))
            {
                string[] parts = path.Split(new char[] { '/' }, 2);
                if (!children.ContainsKey(parts[0]))
                    children.Add(parts[0], new DataBranch());
                var child = (DataBranch)(children[parts[0]]);
                child.addPathedValue(parts[1], value);
            }
            else
            {
                children.Add(path, value);
            }

        }

        public override DataNode this[string key] { get { return children[key]; } set { children[key] = value; } }

        public override bool IsLeaf
        {
            get
            {
                return false;
            }
        }

        public IEnumerable<string> Keys { get { return children.Keys; } }

        public override T As<T>()
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<string, DataNode>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, DataNode>>)children).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, DataNode>>)children).GetEnumerator();
        }

        public bool ContainsKey(string key)
        {
            return children.ContainsKey(key);
        }

        public override bool NotContains(DataNode data)
        {
            if (data.IsLeaf) return true;
            bool differs = false;
            foreach (var item in data.AsBranch())
            {
                if (!ContainsKey(item.Key)) return true;
                DataNode other = item.Value;
                DataNode me = this[item.Key];
                differs = differs || me.NotContains(other);
            }
            return differs;
        }

        public void Clear()
        {
            children.Clear();
        }

        public override void Merge(DataBranch data)
        {
            if (data.IsLeaf) throw new NotImplementedException();
            foreach (var item in data)
            {
                if (ContainsKey(item.Key))
                {
                    if (item.Value == null)
                    {
                        //i think this neve happens.
                        children.Remove(item.Key);
                    }
                    else
                    {
                        if (children[item.Key].IsLeaf != item.Value.IsLeaf || item.Value.IsLeaf)
                        {
                            //this might override the leaf with a new one with the same value.
                            children[item.Key] = item.Value;
                        }
                        else {
                            //i can only get here if i have two branches to merge.
                            children[item.Key].AsBranch().Merge(item.Value.AsBranch());
                        }

                    }
                }
                else
                {
                    if (item.Value != null)
                    {
                        children[item.Key] = item.Value;
                    }
                }
            }
             
        }

        public override DataBranch AsBranch()
        {
            return this;
        }
    }
}
