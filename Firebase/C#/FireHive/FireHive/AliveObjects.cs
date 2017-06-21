using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;
using FireHive.Dynamic;
using Firebase;
using Firebase.Data.Changeset;
using FireHive.Firebase.REST.Data;

namespace FireHive
{
    class AliveObjects : Map<string, object>
    {
        internal event EventHandler<EventArgs> Initialized;
        private FirebaseClient client;
        bool initialized = false;
        public AliveObjects(FirebaseClient client)
        {
            missingReferences = new List<KeyValuePair<string, Action<object>>>();
            this.client = client;

            client.On("objects", SubscribeOperations.Added, childAdded);
            client.On("objects", SubscribeOperations.Changed, childChanged);
            client.On("objects", SubscribeOperations.Removed, childDeleted);

        }


        private void childDeleted(string arg1, ChangeSet arg2)
        {

            if (innerDictionary.ContainsKey(arg1))
                innerDictionary.Remove(arg1);
        }

        private void childAdded(string Key, ChangeSet input)
        {
            //if i already have an object with this key i'll ignore it, and see what happens.
            if (innerDictionary.ContainsKey(Key))
                return;
            object obj = null;
            var type = input["type"].As<string>();
            if (isPrimitiveTypeName(type))
            {
                if (type == "Date")
                {
                    obj = (input["value"].As<DateTime>()).ToLocalTime();
                }
                else
                {
                    //let's say this is a literal for now.
                    obj = input["value"];
                }
                innerDictionary[Key] = obj;
            }
            else if (type == "null")
            {
                innerDictionary[Key] = null;
            }
            else
            {
                //first version.
                if (type == "Array")
                {
                    obj = new List<Object>();
                }
                else
                {
                    obj = new ExpandibleObject(new object());
                }
                innerDictionary[Key] = obj;
                mapSnapshotToObject(obj, input);
            }
            checkForRefrences(Key, obj);

            if (!initialized && missingReferences.Count==0)
            {
                initialized = true;
                Initialized?.Invoke(this, EventArgs.Empty);
            }


        }

        internal string Add(object value)
        {
            if (innerDictionary.ContainsValue(value))
            { return GetKey(value); }
            string key = client.Post("objects");
            value = new Dynamic.ExpandibleObject(value);
            Set(key, value);
            Dictionary<string, object> upd = new Dictionary<string, object>();
            if (value == null)
            {
                upd["/" + key + "/type/"] = "null";
                upd["/" + key + "/value/"] = null;
            }
            else if (!isPrimitive(value))
            {
                UpdateFields(value, getFields(value));
            }
            else
            {
                var type = sanitizeTypeName(value.GetType());
                upd["/" + key + "/type/"] = type;
                var basePath = "/" + key + "/";
                //first of all i need to see if the value is either null or undefined.
                if (type == "Date")
                {
                    upd[basePath + "value"] = ((DateTime)value).ToUniversalTime().ToString("s");
                }
                else
                {
                    upd[basePath + "value"] = value;
                }
            }


            client.Patch("objects", upd);
            return key;
        }

        private IEnumerable<string> getFields(object value)
        {
            if (value.GetType() == typeof(ExpandibleObject))
            {
                return ((ExpandibleObject)value).GetPropertyNames();
            }
            //inheritance may not work correctly here
            return value.GetType().GetProperties().Select(t => t.Name);
        }

        private void checkForRefrences(string key, object obj)
        {
            var toExecute = missingReferences.Where(t => t.Key == key).ToArray();
            missingReferences.RemoveAll(t => t.Key == key);
            foreach (var item in toExecute)
            {
                item.Value(obj);
            }
        }

        List<KeyValuePair<string, Action<object>>> missingReferences;

        private void mapSnapshotToObject(object obj, ChangeSet input)
        {
            string myType = input["type"].As<string>();
            if (myType == "Array")
            {
                List<object> list = (List<object>)obj;
                ChangesetBranch received = null;
                int maxIndex = 0;
                if (input.Childs.ContainsKey("data"))
                {
                    received = (ChangesetBranch)input["data"];
                    maxIndex = received.Childs.Keys.Select(int.Parse).Max();
                }
                //todo: if i remove things from an array this wont update correctly.

                while (list.Count <= maxIndex)
                {
                    list.Add(null);
                }
                //remove elements to ensure the length. 
                //not anymore.  list.RemoveRange(received.Childs.Count(), list.Count - received.Childs.Count());
                foreach (var item in received.Childs)
                {
                    int i = int.Parse(item.Key);
                    string type = item.Value["type"].As<string>();
                    if (isPrimitiveTypeName(type))
                    {

                        list[i] = item.Value["value"].As<object>();
                    }
                    else if (type == "null")
                    {
                        list[i] = null;
                    }
                    else
                    {
                        //object or array.
                        string otherKey = item.Value["value"].As<string>();
                        var other = Get(otherKey);
                        if (other == null)
                        {
                            missingReferences.Add(new KeyValuePair<string, Action<object>>(otherKey, (otherObj) =>
                            {
                                list[i] = otherObj;
                            }));
                        }
                        else
                        {
                            list[i] = other;
                        }

                    }
                }
            }
            else
            {

                IDictionary<string, object> dictionary = ((ExpandibleObject)obj).asDictionary();
                if (input.Childs.ContainsKey("data"))
                {
                    ChangesetBranch dataBranch = (ChangesetBranch)input["data"];
                    foreach (var item in dataBranch.Childs)
                    {
                        string type = "";
                        if (item.Value.Childs.ContainsKey("type"))
                            type = item.Value["type"].As<string>();
                        else
                            type = sanitizeTypeName(dictionary[item.Key].GetType());
                        if (isPrimitiveTypeName(type))
                        {

                            dictionary[item.Key] = item.Value["value"].As<object>();
                        }
                        else if (type == "null")
                        {

                            dictionary[item.Key as string] = null;
                        }
                        else
                        {
                            //object or array.
                            string otherKey = item.Value["value"].As<string>();
                            var other = Get(otherKey);
                            if (other == null)
                            {
                                missingReferences.Add(new KeyValuePair<string, Action<object>>(otherKey, (otherObj) =>
                                {
                                    dictionary[item.Key] = otherObj;
                                }));
                            }
                            else
                            {
                                dictionary[item.Key] = other;
                            }
                        }
                    }
                }
            }

        }

        private void childChanged(string key, ChangeSet data)
        {
            var obj = innerDictionary[key];
            //todo: massive hack
            if (obj is IList<object>) { data["type"] = new ChangeSetLeaf("Array"); } else { data["type"] = new ChangeSetLeaf("Object"); }

            mapSnapshotToObject(obj, data);
        }


        //private void childRemoved(FirebaseEvent<DataTypes.DataObject> t)
        //{
        //    throw new NotImplementedException();
        //}
        public void UpdateField(object obj, string fieldName)
        { UpdateFields(obj, new string[] { fieldName }); }
        public void UpdateFields(object obj, IEnumerable<string> fieldNames)
        {
            Dictionary<string, object> upd = new Dictionary<string, object>();
            var id = GetKey(obj);
            if (id != null)
            {
                upd["/" + id + "/type"] = sanitizeTypeName(obj.GetType());
                foreach (var fieldName in fieldNames)
                {
                    string basePath = "/" + id + "/data/" + fieldName + "/";
                    object value = getPropertyValue(obj, fieldName);
                    if (value == null)
                    {
                        upd[basePath + "type"] = "null";
                        upd[basePath + "value"] = null;
                    }
                    else
                    {
                        string type = sanitizeTypeName(value.GetType());
                        upd[basePath + "type"] = type;
                        if (isPrimitive(value))
                        {
                            if (type == "Date")
                            {
                                upd[basePath + "value"] = ((DateTime)value).ToUniversalTime().ToString("s");
                            }
                            else
                            {
                                upd[basePath + "value"] = value;
                            }
                        }
                        else
                        {
                            //obj.
                            //ensure i have no proxies here.
                            value = Hive.Current.UnProxyfy(value);
                            setPropertyValue(obj, fieldName, value);

                            if (type == "Object" || type == "Array")
                            {
                                upd[basePath + "value"] = Add(value);
                            }
                        }
                    }


                }
                client.Patch("objects", upd);
            }

        }

        private void setPropertyValue(object rcvr, string name, object value)
        {
            if (rcvr.GetType() == typeof(List<object>))
            {
                List<object> list = (List<object>)rcvr;
                list[int.Parse(name)] = value;
                return;
            }
            try
            {
                ((IDictionary<string, object>)rcvr)[name] = value;
            }
            catch (InvalidCastException)
            {
                rcvr.GetType().GetProperty(name).SetValue(rcvr, value);

            }
        }

        private object getPropertyValue(object rcvr, string name)
        {
            //todo: what if what i receive is not a dictionary.
            //if i have to get it from an array it is different
            if (rcvr.GetType() == typeof(List<object>))
            {
                //let's try it with a simple parse.
                var list = (IList<object>)rcvr;
                return list[int.Parse(name)];
            }
            try
            {
                if (rcvr.GetType() == typeof(ExpandibleObject))
                {
                    object result;
                    ((ExpandibleObject)(rcvr)).innerGet(name, out result);
                    return result;
                }
                return ((IDictionary<string, object>)rcvr)[name];
            }
            catch (InvalidCastException)
            {
                return rcvr.GetType().GetProperty(name).GetValue(rcvr);

            }
            //return rcvr.GetType().GetProperty(name).GetValue(rcvr);
        }
        private string sanitizeTypeName(Type type)
        {
            if (type == typeof(DateTime))
                return "Date";
            if (type == typeof(string))
                return "String";
            if (type == typeof(bool))
                return "Boolean";
            if (type == typeof(int) || type == typeof(long) || type == typeof(double) || type == typeof(float))
                return "Number";
            if (type == typeof(List<object>) || type.IsArray)
                return "Array";
            return "Object";
        }

        bool isPrimitive(object obj)
        {
            if (obj == null)
            { return false; }
            //todo: numbers and stuff have different names here. i should map them.
            return isPrimitiveTypeName(sanitizeTypeName(obj.GetType()));
        }
        bool isPrimitiveTypeName(string name)
        {
            return name == "Number" ||
                name == "Date" ||
                name == "Boolean" ||
                name == "String";
        }
    }
}
