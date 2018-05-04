using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Tera.ChromeDevTools;

namespace HiveTests
{ 
    [TestClass]
    public class ValueTests
    {
        Chrome chrome;
        public ValueTests()
        {
            chrome = new Chrome(headless: false);
        }
        ~ValueTests()
        {
            chrome.Dispose();
        }

        [TestMethod]
        public async Task NumberSyncTest()
        {
            List<ChromeSession> sessions =  await chrome.CreateHiveSessions(2);
            await sessions[0].hiveSetValue("number", 5);
            await Task.Delay(10);
            Assert.AreEqual(5, await sessions[1].hiveGetValue<int>("number"), 0, "The value was not present on the second session!");
        }

    }

}
