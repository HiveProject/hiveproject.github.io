using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;
using FireHive.DataTypes;

namespace FireHive
{
    class AliveObjects : Map<string, object>
    {
      //  private FirebaseClient database;
        public AliveObjects(/*FirebaseClient database*/)
        {
            missingReferences = new List<KeyValuePair<string, Action<object>>>();
           /* this.database = database;
            database.Child("objects").AsObservable<DataTypes.DataObject>().Subscribe(t =>
            {
                if (t.Key != string.Empty)
                    switch (t.EventType)
                    {
                        case FirebaseEventType.InsertOrUpdate:
                            if (innerDictionary.ContainsKey(t.Key))
                            {
                                //this library sucks, it does not differentiate correctly adds from updates
                                //and to make it workst, it triggers this event  more times than it should.
                                childChanged(t);
                            }
                            else
                            {
                                childAdded(t);
                            }
                            break;
                        case FirebaseEventType.Delete:
                            childRemoved(t);
                            break;
                        default:
                            break;
                    }

            });*/
        }

        //private void childAdded(FirebaseEvent<DataTypes.DataObject> dataSnapshot)
        //{
        //    object obj = null;
        //    var received = dataSnapshot.Object;
        //    var type = received.type;
        //    if (isPrimitiveTypeName(type))
        //    {
        //        if (type == "Date")
        //        {

        //            obj = ((DateTime)received.value).ToLocalTime();
        //        }
        //        else
        //        {
        //            //let's say this is a literal for now.
        //            obj = received.value;
        //        }
        //        innerDictionary[dataSnapshot.Key] = obj;
        //    }
        //    else if (type == "null")
        //    {
        //        innerDictionary[dataSnapshot.Key] = null;
        //    }
        //    else
        //    {
        //        //first version.
        //        if (type == "Array")
        //        {
        //            obj = new List<Object>();
        //        }
        //        else
        //        {
        //            obj = new ExpandoObject();
        //        }
        //        // Activator.CreateInstance()
        //        //if (eval("typeof(" + received.type + ")") != "undefined")
        //        //{
        //        //    obj = eval("new " + received.type + "();");
        //        //}
        //        //else { obj ={ }; }
        //        innerDictionary[dataSnapshot.Key] = obj;
        //        mapSnapshotToObject(obj, received);
        //    }
        //    checkForRefrences(dataSnapshot.Key, obj);
        //}

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
        private void mapSnapshotToObject(object obj, DataObject input)
        {

            dynamic received = input;
            IDictionary<string, object> dictionary = (IDictionary<string, object>)obj;
            foreach (var item in received.data)
            {
                if (isPrimitiveTypeName(item.Value.type))
                {

                    dictionary[item.Key as string] = item.Value.value;
                }
                else if (item.Value.type == "null")
                {

                    dictionary[item.Key as string] = null;
                }
                else
                {
                    //object or array.
                    string otherKey = item.Value.value.ToString();
                    var other = Get(otherKey);
                    if (other == null)
                    {
                        missingReferences.Add(new KeyValuePair<string, Action<object>>(otherKey, (otherObj) =>
                        {
                            dictionary[item.Key as string] = otherObj;
                        }));
                    }
                    else
                    {
                        dictionary[item.Key as string] = other;
                    }

                }


            }

        }

        //private void childChanged(FirebaseEvent<DataTypes.DataObject> t)
        //{
        //    if (!isPrimitiveTypeName(t.Object.type))
        //    {
        //        var obj = innerDictionary[t.Key];
        //        //this CANNOT be called for primitive types.
        //        mapSnapshotToObject(obj, t.Object);
        //    }
        //}


        //private void childRemoved(FirebaseEvent<DataTypes.DataObject> t)
        //{
        //    throw new NotImplementedException();
        //}

        bool isPrimitive(object obj)
        {
            if (obj == null)
            { return false; }
            //todo: numbers and stuff have different names here. i should map them.
            return isPrimitiveTypeName(obj.GetType().Name);
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
