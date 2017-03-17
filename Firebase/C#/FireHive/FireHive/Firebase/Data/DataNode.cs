using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireHive.Firebase.Data
{
    abstract class DataNode
    {
        public static DataNode FromJsonString(string source)
        {
            return mapJsonToken(JToken.Parse(source));

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
                    return new DataLeaf(((JValue)token).Value);

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
                    Queue<string> path = new Queue<string>(item.Split('/').Where(t => t != ""));
                    var child = branch[item];
                    if (child.IsLeaf)
                    {
                        toUpdate[item] = ((DataLeaf)this).Value;
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

        public abstract bool Differs(DataNode data);

        public abstract void Merge(DataNode data);

        public abstract DataBranch AsBranch();
    }
}
