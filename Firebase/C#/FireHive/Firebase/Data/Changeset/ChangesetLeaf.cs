using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Firebase.Data.Changeset
{
    public class ChangeSetLeaf : ChangeSet
    {
        public ChangeSetLeaf(object value)
        {
            if (value.GetType() == typeof(int))
                value = Convert.ToInt64(value);

            Value = value;
        }



        public override bool IsLeaf
        {
            get
            {
                return true;
            }
        }

        public object Value { get; set; }

        internal override DataNode ToDataNode()
        {
            return new DataLeaf(Value);
        }
    }
}
