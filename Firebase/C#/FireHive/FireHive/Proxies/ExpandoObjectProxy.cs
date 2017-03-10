using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace FireHive.Proxies
{
    public class ExpandoObjectProxy : DynamicObject
    {
        //todo, i do not need the expandoobject anymore, i can have the same functionality directly with a dictionary here.
        public ExpandoObjectProxy(object instance, Action<object,string> SetExecuted)
        {
            setExecuted=SetExecuted;

            realInstance = instance;
            objectDictionary = (IDictionary<string, object>)realInstance;
        }
        private dynamic realInstance;
        private IDictionary<string, object> objectDictionary;
        private Action<object, string> setExecuted;

        public override string ToString()
        {
            return realInstance.ToString();
        }
        public override int GetHashCode()
        {
            return realInstance.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(ExpandoObjectProxy))
            {
                var other = (ExpandoObjectProxy)obj;
                return realInstance.Equals(other.realInstance);
            }
            return realInstance.Equals(obj);
        }





        //properties
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (!objectDictionary.ContainsKey(binder.Name))
            {
                result = null;
            }
            else {
                result = Hive.Current.getProxy(objectDictionary[binder.Name]);
            }
            return true;
        }
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (Hive.Current.proxies.ContainsValue(value))
            {
                value = Hive.Current.proxies.FirstOrDefault(kvp => kvp.Value == value).Key;
            }
            objectDictionary[binder.Name] = value;
            setExecuted(realInstance, binder.Name);
            return true;
        }

        //array?
        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            return base.TryGetIndex(binder, indexes, out result);
        }
        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            return base.TrySetIndex(binder, indexes, value);
        }
        public override bool TryDeleteIndex(DeleteIndexBinder binder, object[] indexes)
        {
            return base.TryDeleteIndex(binder, indexes);
        }




        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return objectDictionary.Keys;
        }

        public override DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return base.GetMetaObject(parameter);
        }
        public override bool TryBinaryOperation(BinaryOperationBinder binder, object arg, out object result)
        {
            return base.TryBinaryOperation(binder, arg, out result);
        }
        public override bool TryUnaryOperation(UnaryOperationBinder binder, out object result)
        {
            return base.TryUnaryOperation(binder, out result);
        }
        public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
        {
            return base.TryInvoke(binder, args, out result);
        }
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            return base.TryInvokeMember(binder, args, out result);
        }
        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            return base.TryConvert(binder, out result);
        }


    }
}
