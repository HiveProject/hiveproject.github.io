using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic; 
using System.Text;
using System.Threading.Tasks;
using Tera.ChromeDevTools;

namespace HiveTests
{
    [TestClass]
    public class ObjectTests
    {
        Chrome chrome;
        [TestInitialize]
        public void Initialize()
        {
            chrome = new Chrome(headless: false);
        }
        [TestCleanup]
        public void Cleanup()
        {
            chrome.Dispose();
            chrome = null;
        }

        [TestMethod]
        public async Task ObjectSyncTest()
        {
            List<ChromeSession> sessions = await chrome.CreateHiveSessions(2);
            var p = new Point(10, 20);
            await sessions[0].hiveSet("obj1", p);
            await sessions[1].hiveWaitUntilGet("obj1",  5000);
            Assert.IsTrue( await sessions[1].EvalValue<bool>("var p = hive.get(\"obj1\"); p.x ==10 &&p.y==20;" ),"the object was not correctly replicated");
        }

    }

    public struct Point
    {
       public int x;
       public int y; 

        public Point(int x, int y) 
        {
            this.x = x;
            this.y = y;
        }
    }
}
