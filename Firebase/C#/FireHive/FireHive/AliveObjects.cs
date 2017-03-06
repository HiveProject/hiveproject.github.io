using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;
using FireHive.Firebase;

namespace FireHive
{
    class AliveObjects : Map<string, object>
    {
        private FirebaseClient client;
        public AliveObjects(FirebaseClient client)
        {
            missingReferences = new List<KeyValuePair<string, Action<object>>>();
            this.client = client;

            client.On("objects", FirebaseEvent.Added, childAdded);
            client.On("objects", FirebaseEvent.Changed, childChanged);
            client.On("objects", FirebaseEvent.Deleted, childDeleted);
            
        }

        private void childDeleted(string arg1, Dictionary<string, object> arg2)
        {

            if (innerDictionary.ContainsKey(arg1))
                innerDictionary.Remove(arg1);
        }

        private void childAdded(string Key, Dictionary<string, object> input)
        {

            object obj = null;
            var type = input["type"].ToString();
            if (isPrimitiveTypeName(type))
            {
                if (type == "Date")
                {
                    obj = ((DateTime)input["value"]).ToLocalTime();
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
                    obj = new ExpandoObject();
                }
                // Activator.CreateInstance()
                //if (eval("typeof(" + received.type + ")") != "undefined")
                //{
                //    obj = eval("new " + received.type + "();");
                //}
                //else { obj ={ }; }
                innerDictionary[Key] = obj;
                mapSnapshotToObject(obj, input);
            }
            checkForRefrences(Key, obj);
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

        private void mapSnapshotToObject(object obj, Dictionary<string, object> input)
        {
            string myType = input["type"].ToString();
            if (myType == "Array")
            {
                List<object> list = (List<object>)obj;
                var received = input["data"].asDictionary();
                //todo: if i remove things from an array this wont update correctly.

                //if (list.Count > received.Count)
                //{
                //    list.RemoveRange(received.Count - 1, list.Count - received.Count);
                //}
                int maxIndex = input["data"].asDictionary().Keys.Select(int.Parse).Max();
                while (list.Count <= maxIndex)
                {
                    list.Add(null);
                }
                foreach (var item in received)
                {
                    int i = int.Parse(item.Key);
                    string type = item.Value.asDictionary()["type"].ToString();
                    if (isPrimitiveTypeName(type))
                    {

                        list[i] = item.Value.asDictionary()["value"];
                    }
                    else if (type == "null")
                    {
                        list[i] = null;
                    }
                    else
                    {
                        //object or array.
                        string otherKey = item.Value.asDictionary()["value"].ToString();
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
                IDictionary<string, object> dictionary = (IDictionary<string, object>)obj;
                foreach (var item in (Dictionary<string, object>)input["data"])
                {
                    string type = item.Value.asDictionary()["type"].ToString();
                    if (isPrimitiveTypeName(type))
                    {

                        dictionary[item.Key as string] = item.Value.asDictionary()["value"];
                    }
                    else if (type == "null")
                    {

                        dictionary[item.Key as string] = null;
                    }
                    else
                    {
                        //object or array.
                        string otherKey = item.Value.asDictionary()["value"].ToString();
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

        private void childChanged(string key, Dictionary<string, object> data)
        {
            var obj = innerDictionary[key];
            //todo: massive hack
            if (obj is IList<object>) { data["type"] = "Array"; } else { data["type"] = "Object"; }

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
                upd["/" + id + "/type/"] = sanitizeTypeName(obj.GetType());
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
                        upd[basePath + "/type/"] = type;
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
                        }
                    }


                }
                client.Patch("objects",upd);
            }

        }


        private object getPropertyValue(object rcvr, string name)
        {
            //todo what if what i receive is not a dictionary.
            return ((IDictionary<string, object>)rcvr)[name];
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
            if (type.IsArray)
                return "Array";
            return "Object";
        }

        bool isPrimitive(object obj)
        {
            if (obj == null)
            { return false; }
            //todo: numbers and stuff have different names here. i should map them.
            return isPrimitiveTypeName( sanitizeTypeName( obj.GetType()));
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
