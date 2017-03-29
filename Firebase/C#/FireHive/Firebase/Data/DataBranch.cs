using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Firebase.Data.Changeset;

namespace Firebase.Data
{
    internal class DataBranch : DataNode, IEnumerable<KeyValuePair<string, DataNode>>
    {
        private Dictionary<string, DataNode> children;
        public DataBranch()
        {
            children = new Dictionary<string, DataNode>();
            Path = "";
        }
        public DataBranch(Dictionary<string, DataNode> childs)
        {
            children = new Dictionary<string, DataNode>();
            Path = "";
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
                    setChild(parts[0], new DataBranch());
                var child = (DataBranch)(children[parts[0]]);
                child.addPathedValue(parts[1], value);
            }
            else
            {
                setChild(path, value);
            }

        }

        public override DataNode this[string key] { get { return children[key]; } set { setChild(key, value); } }

        public override bool IsLeaf
        {
            get
            {
                return false;
            }
        }

        #region ienumerable
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

        public override bool ContainsKey(string key)
        {
            return children.ContainsKey(key);
        }
        #endregion
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



        public override string Path
        {
            get
            {
                return base.Path;
            }

            set
            {
                base.Path = value;
                foreach (var key in Keys)
                {
                    children[key].Path = Path + "/" + key;
                }
            }
        }
        private void setChild(string key, DataNode child)
        {
            children[key] = child;
            if (Path != "")
            {
                child.Path = Path + "/" + key;
            }
            else
            {
                child.Path = key;
            }
        }

        public override DataBranch AsBranch()
        {
            return this;
        }

        public override DataNode Find(string path, bool create = false)
        {

            //if the path points somewhere outside of myself i must return null.
            if (path.StartsWith(Path))
            { //i am in the same route, and either i am the node, or one of my children is
                if (Path.Equals(path))
                    return this;
                //find next child.
                var subpath = path.Substring(Path.Length);
                //remove initial dash.
                if (subpath.StartsWith("/"))
                    subpath = subpath.Substring(1);
                int end = subpath.IndexOf("/");
                if (end != -1)
                {
                    subpath = subpath.Substring(0, end);
                }
                if (!ContainsKey(subpath))
                    if (create)
                        setChild(subpath, new DataBranch());
                    else
                        return null;
                return children[subpath].Find(path);

            }
            else { return null; }
        }

        internal override void Merge(ChangeSet data)
        {
            // i do not know how to join this, my parent should have joined this.
            if (data.IsLeaf) throw new NotImplementedException();
            foreach (var kvp in data.Childs)
            {
                if (ContainsKey(kvp.Key))
                {
                    //i have the key
                    if (kvp.Value == null)
                    {
                        //if i have the key, and the new value is null, is because this was removed
                        children.Remove(kvp.Key);
                        kvp.Value.Type = ChangeType.Removed;
                    }
                    else
                    {
                        //this is a modification
                        if (children[kvp.Key].IsLeaf != kvp.Value.IsLeaf  )
                        {
                            //what i had, and what it is suposed to have now are not the same type.
                            setChild(kvp.Key, kvp.Value.ToDataNode());
                            kvp.Value.Type = ChangeType.Modified;

                        }
                        else
                        {
                            //i can only get here if i have two branches to merge.
                            //the recursion will handle the rest.
                            children[kvp.Key].Merge(kvp.Value);
                        }

                    }
                }
                else
                {
                    //i do not have the key. this is an Add.
                    setChild(kvp.Key, kvp.Value.ToDataNode());
                    kvp.Value.Type = ChangeType.Added;
                }

            }

        }
    }
}
