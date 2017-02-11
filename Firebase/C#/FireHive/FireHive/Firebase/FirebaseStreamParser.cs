using System;
using System.IO;

namespace FireHive.Firebase
{
    internal class FirebaseStreamParser
    {
        public StreamReader Reader { get; internal set; }

        internal void On(FirebaseEvent evt, Action<object> callback)
        {
            throw new NotImplementedException();
        }

        internal void Start()
        {
            throw new NotImplementedException();
        }
    }
}