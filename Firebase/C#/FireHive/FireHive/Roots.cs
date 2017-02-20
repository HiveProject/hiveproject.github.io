using FireHive.Firebase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireHive
{
    class Roots : Map<string, string>
    {
        private FirebaseClient client;
        private List<KeyValuePair<string, Action<object>>> missingReferences;

        public Roots(FirebaseClient client)
        {
            missingReferences = new List<KeyValuePair<string, Action<object>>>();
            this.client = client;

            client.On("roots", FirebaseEvent.Added, childAdded);
            client.On("roots", FirebaseEvent.Changed, childChanged);

            client.On("roots", FirebaseEvent.Deleted, childDeleted);
        }

        private void childDeleted(string arg1, Dictionary<string, object> arg2)
        {
            throw new NotImplementedException();
        }

        private void childChanged(string arg1, Dictionary<string, object> arg2)
        {
            innerDictionary[arg1] = arg2["value"] as string;
        }

        private void childAdded(string arg1, Dictionary<string, object> arg2)
        {
            innerDictionary[arg1]= arg2["value"] as string;
        }


    }
}
