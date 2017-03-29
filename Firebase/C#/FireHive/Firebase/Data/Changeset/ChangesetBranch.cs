using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Firebase.Data.Changeset
{
    public class ChangesetBranch : ChangeSet
    {
        public ChangesetBranch(Dictionary<string, ChangeSet> childs)
        {
            foreach (var item in childs)
            {
                Childs[item.Key] = item.Value;
            }
        }


        public override bool IsLeaf
        {
            get
            {
                return false;
            }
        }
        public override ChangeType Type
        {
            get
            {
                if (base.Type == ChangeType.Undefined)
                {
                    if (Childs.Any(kvp => kvp.Value.Type != ChangeType.Undefined))
                    {
                        if (Childs.Any(kvp => kvp.Value.Type != ChangeType.None))
                        {
                            return ChangeType.Modified;
                        }
                        else { return ChangeType.None; }
                    }
                }
                return base.Type;

            }

            set
            {
                base.Type = value;
                //if i was added or removed, al my childs were added or removed too.
                if (value == ChangeType.Added || value == ChangeType.Removed)
                    foreach (var item in Childs)
                    {
                        item.Value.Type = value;
                    }
            }
        }

        public override T As<T>()
        {
            throw new NotImplementedException();
        }

        internal override DataNode ToDataNode()
        {

            var branch = new DataBranch();
            foreach (var item in Childs)
            {
                branch[item.Key] = item.Value.ToDataNode();

            }

            return branch;

        }
    }
}
