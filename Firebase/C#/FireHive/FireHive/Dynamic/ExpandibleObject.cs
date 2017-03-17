using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FireHive.Dynamic
{
    public class ExpandibleObject : System.Dynamic.DynamicObject
    {
        Dictionary<string, object> extendedObject;
        object innerObject;

        public IDictionary<string, object> asDictionary()
        {

            return new ExpandibleObjectDictionary(this);
        }

        public ExpandibleObject(object InnerObject)
        {
            if (InnerObject == null)
                throw new InvalidOperationException();
            innerObject = InnerObject;
            extendedObject = new Dictionary<string, object>();
        }
        internal IEnumerable<string> GetPropertyNames()
        {
            return GetProperties().Select(p => p.Name).Union(extendedObject.Keys);
        }
        private IEnumerable<PropertyInfo> GetProperties()
        {
            //todo: cache maybe?
            return innerObject.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return innerObject.GetType().GetMembers().Select(m => m.Name).Union(extendedObject.Keys);
        }

        internal bool innerGet(string name, out object result)
        {
            var property = GetProperties().FirstOrDefault(p => p.Name == name);
            if (property != null)
            {
                result = property.GetValue(innerObject);
                return true;
            }
            else
            {
                if (extendedObject.ContainsKey(name))
                {
                    result = extendedObject[name];
                    return true;
                }
            }
            result = null;
            return false;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            //todo: cache this maybe?
            return innerGet(binder.Name, out result);

        }
        internal bool innerSet(string name, object value)
        {
            var property = GetProperties().FirstOrDefault(p => p.Name == name);
            if (property != null)
            {//the property exists in the real object.

                //if the type of value is not compatible with the type of the property, then here is where i am going to enforce a rollback.
                property.SetValue(innerObject, value);

                return true;
            }
            else
            {
                extendedObject[name] = value;
                return true;
            }
        }
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            return innerSet(binder.Name, value);
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
        {
            return base.TryInvoke(binder, args, out result);

        }
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            //find the member.

            var member = innerObject.GetType().GetMember(binder.Name).FirstOrDefault();
            if (member != null)
            {
                result = innerObject.GetType().InvokeMember(member.Name, BindingFlags.InvokeMethod, null, innerObject, args);
                return true;

            }
            result = null;
            return false;

        }
        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            return base.TryConvert(binder, out result);
        }

     
    }
}
