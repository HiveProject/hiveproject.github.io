using Firebase;
using Firebase.Data.Changeset;
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

            client.On("roots", SubscribeOperations.Added, childAdded);
            client.On("roots", SubscribeOperations.Changed, childChanged);

            client.On("roots", SubscribeOperations.Removed, childDeleted);
        }
        public override bool Set(string key, string value)
        {

            client.Patch("roots", new Dictionary<string, object>() { { key, value } });
            return true;
        }
        private void childDeleted(string arg1, ChangeSet arg2)
        {
            if (innerDictionary.ContainsKey(arg1))
                innerDictionary.Remove(arg1);
        }

        private void childChanged(string arg1, ChangeSet arg2)
        {
            if (!arg2.IsLeaf)
                throw new NotImplementedException("this was suposed to be a leaf!");
            var leaf = (ChangeSetLeaf)arg2;

            innerDictionary[arg1] = leaf.Value.ToString();
        }

        private void childAdded(string arg1, ChangeSet arg2)
        {
            if (!arg2.IsLeaf)
                throw new NotImplementedException("this was suposed to be a leaf!");
            var leaf = (ChangeSetLeaf)arg2;

            innerDictionary[arg1] = leaf.Value.ToString();
        }


    }
}
