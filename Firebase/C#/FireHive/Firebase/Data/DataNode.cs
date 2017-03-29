using Firebase.Data.Changeset;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Firebase.Data
{
    internal abstract class DataNode
    {
        public static DataNode FromJsonString(string source)
        {
            return mapJsonToken(JToken.Parse(source));

        }
        public static DataNode FromJToken(JToken source)
        {
            return mapJsonToken(source);

        }
        private static DataNode mapJsonToken(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    return new DataBranch(token.Children<JProperty>().ToDictionary(prop => prop.Name,
                                              prop => mapJsonToken(prop.Value)));
                case JTokenType.Array:

                    var i = 0;
                    return new DataBranch(token.Select(mapJsonToken).ToDictionary(p => (i++).ToString(), p => p));

                default:
                    object value = ((JValue)token).Value;
                    if (value == null) return null;
                    return new DataLeaf(value);

            }
        }


        public abstract bool IsLeaf { get; }
        public abstract DataNode this[string key] { get; set; }

        public abstract T As<T>();

        public IDictionary<string, object> FlattenDictionary()
        {
            Dictionary<string, object> toUpdate = new Dictionary<string, object>();
            if (IsLeaf)
            {
                toUpdate.Add("value", ((DataLeaf)this).Value);
            }
            else
            {
                DataBranch branch = (DataBranch)this;
                foreach (var item in branch.Keys)
                {
                    var child = branch[item];
                    if (child.IsLeaf)
                    {
                        toUpdate[item] = ((DataLeaf)child).Value;
                    }
                    else
                    {
                        foreach (var childItem in child.FlattenDictionary())
                        {
                            toUpdate[item + "/" + childItem.Key] = childItem.Value;
                        }
                    }

                }
            }

            return toUpdate;


        }

        public virtual string Path { get; set; }

        public abstract bool NotContains(DataNode data);



        internal abstract void Merge(ChangeSet data);



        public abstract DataNode Find(string path,bool create=false);

        public abstract DataBranch AsBranch();

        public abstract bool ContainsKey(string key);


        public DataNode Parent { get; set; }
    }
}
