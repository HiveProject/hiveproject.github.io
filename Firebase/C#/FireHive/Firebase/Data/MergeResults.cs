using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Firebase.Data
{
    internal class MergeResults
    {
        public MergeResults()
        {
            Added = new List<DataBranch>();
            Changed = new List<DataBranch>();
            Removed = new List<DataBranch>();
        }
        public List<DataBranch> Added { get; set; }
        public List<DataBranch> Changed { get; set; }
        public List<DataBranch> Removed { get; set; }
    }
}
