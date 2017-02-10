using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireHive.DataTypes
{
    class DataObject
    {
        public string type { get; set; }
        public Dictionary<object,DataProperty> data { get; set; }
        public object value { get; set; }
    }
}
