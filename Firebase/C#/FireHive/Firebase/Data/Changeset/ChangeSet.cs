using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Firebase.Data.Changeset
{
    public abstract class ChangeSet
    {
        public ChangeSet()
        { childs = new Dictionary<string, ChangeSet>(); }
        public string Path { get; set; }
        ChangeType? type;
        public virtual ChangeType Type
        {
            get
            {
                if (type == null)
                    return ChangeType.Undefined;
                return type.Value;
            }
            set { type = value; }
        }
        Dictionary<string, ChangeSet> childs;
        public Dictionary<string, ChangeSet> Childs { get { return childs; } }
        public abstract bool IsLeaf { get; }
        public static ChangeSet FromJToken(JToken token)
        { return mapJsonToken(token); }
        private static ChangeSet mapJsonToken(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    return new ChangesetBranch(token.Children<JProperty>().ToDictionary(prop => prop.Name,
                                              prop => mapJsonToken(prop.Value)));
                case JTokenType.Array:

                    var i = 0;
                    return new ChangesetBranch(token.Select(mapJsonToken).ToDictionary(p => (i++).ToString(), p => p));

                default:
                    object value = ((JValue)token).Value;
                    if (value == null) return null;
                    return new ChangeSetLeaf(value);

            }

        }

        internal abstract DataNode ToDataNode();

        internal ChangeSet Find(string key)
        {
            int end = key.IndexOf("/");
            string me;
            if (end != -1)
            { me = key.Substring(0, end); }
            else { me = key; }
            if (me == "")
                return this;
            return Childs[me].Find(key.Substring(me.Length));
        }

        internal static ChangeSet FromFlatJToken(JToken token)
        {
            ChangesetBranch branch = new ChangesetBranch(new Dictionary<string, ChangeSet>());

            foreach (var item in token.Children<JProperty>())
            {
                branch.addPathedValue(item.Name, ((JValue)item.Value).Value);
            }
            return branch;
        }

        private void addPathedValue(string path, object value)
        {
            if (path.StartsWith("/"))
                path = path.Substring(1);
            if (path.Contains('/'))
            {
                string[] parts = path.Split(new char[] { '/' }, 2);
                if (!Childs.ContainsKey(parts[0]))
                    childs.Add(parts[0], new ChangesetBranch(new Dictionary<string, ChangeSet>()));
                var child = (ChangesetBranch)(Childs[parts[0]]);
                child.addPathedValue(parts[1], value);
            }
            else
            {
                childs[path] = new ChangeSetLeaf(value);
            }

        }
    }
}
