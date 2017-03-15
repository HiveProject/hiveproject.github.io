using FireHive.Dynamic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireHive
{
    static class  Extensions
    {
        public static IDictionary<string, object> asDictionary(this object self)
        {
            if (self.GetType() == typeof(ExpandibleObject))
                return new ExpandibleObjectDictionary((ExpandibleObject)self);
            return self as Dictionary<string, object>; }
    }
}
