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
        [TestInitialize]
        public void Initialize()
        {
            chrome = new Chrome(headless: false);
        }
        [TestCleanup]
        public void Cleanup() {
            chrome.Dispose();
            chrome = null;
        }
        private async Task TestHiveValueReplication<T>(string key, T value) where T : struct
        {
            List<ChromeSession> sessions = await chrome.CreateHiveSessions(2);
            try
            {

                var result = await sessions[0].hiveSetValue(key, value);
                await Task.Delay(10);

                //check if what returned from both the set and the get are the same.
                Assert.AreEqual(result, await sessions[1].hiveGetValue<T>(key), key+": The value was not present on the second session!");
                //cleanup?
                await sessions[0].hiveRemove(key);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                foreach (var item in sessions)
                {
                    item.Dispose();
                }
            }
        }

        [TestMethod]
        public async Task NumberSyncTest()
        {
            await TestHiveValueReplication("number", 5);
            await TestHiveValueReplication("number", 5.01);
            await TestHiveValueReplication("number", 0.01);
            await TestHiveValueReplication("number", -1);
        }
        [TestMethod]
        public async Task BoolSyncTest()
        {
            await TestHiveValueReplication("bool", true);
            await TestHiveValueReplication("bool", false);
        }
    }

}
