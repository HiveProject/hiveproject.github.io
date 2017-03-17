using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireHive.Firebase.Data
{
    class DataBranch : DataNode,IEnumerable<KeyValuePair<string, DataNode>>
    {
        private Dictionary<string, DataNode> children;
        private DataBranch()
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
    }
}
