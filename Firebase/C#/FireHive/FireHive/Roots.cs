using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireHive
{
    class Roots : Map<string, string>
    {
       // private FirebaseClient database;

      /*  public Roots(Firebase.Database.FirebaseClient db)
        {
            database = db;
            database.Child("roots").AsObservable<string>().Subscribe(t =>
            {
                if (t.Key != string.Empty)
                    switch (t.EventType)
                    {
                        case Firebase.Database.Streaming.FirebaseEventType.InsertOrUpdate:
                            rootAdded(t);
                            break;
                        case Firebase.Database.Streaming.FirebaseEventType.Delete:
                            rootRemoved(t);
                            break;
                        default:
                            break;
                    }
            });
        }*/
        //private void rootRemoved(FirebaseEvent<string> t)
        //{
        //    if (innerDictionary.ContainsKey(t.Key))
        //        innerDictionary.Remove(t.Key);
        //}
        //private void rootAdded(FirebaseEvent<string> t)
        //{
        //    innerDictionary[t.Key] = t.Object;
        //}
        //public override bool Set(string key, string value)
        //{
        //    var result = base.Set(key, value);
        //    database.Child("roots/"+key).PutAsync(value).Wait();
        //    return result;
        //}
        //public override bool Delete(string key)
        //{
        //    var result = base.Delete(key);
        //    database.Child("roots/" + key).DeleteAsync().Wait();
        //    return result;
        //}

    }
}
