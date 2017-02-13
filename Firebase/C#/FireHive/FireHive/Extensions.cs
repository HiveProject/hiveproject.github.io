using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireHive
{
    static class  Extensions
    {
        public static Dictionary<string, object> asDictionary(this object self)
        { return self as Dictionary<string, object>; }
    }
}
