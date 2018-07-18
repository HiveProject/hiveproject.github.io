using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tera.ChromeDevTools;

namespace HiveTests
{

    [TestClass]
    public class ConcurrencyTest
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

        struct counter
        {
            public int value;
            public counter(int v) : this()
            {
                this.value = v;
            }
        }
        [TestMethod]
        public async Task NonGuaranteedInterleavingTest()
        {
            int steps = 1000;
            int nodes = 2;
            string declaration = "var sum = hive.get(\"sum\");";

            string code =
               "function f(){" +
                   "for(let i=0;i<=" + steps + ";i++){" +
                   "sum.value++;" +
                    "}" +
                  "}";
            var sessions = await chrome.CreateHiveSessions(nodes);
            await sessions[0].hiveSet("sum", new counter(0));
            //propagation. 
            await Task.WhenAll(sessions.Select(async s => await s.EvalObject(declaration)));
            sessions.ForEach(async s => await s.EvalObject(code));

            await Task.WhenAll(sessions.Select(s => s.EvalObject("f()")));
            //check the final result?
            await Task.Delay(1000);
            int total = await sessions[0].hiveWaitUntilGetValue<int>("sum", 10000);
            Assert.IsTrue(total <= steps * nodes, "The value exceeded tha max number");
            Assert.IsTrue(total >= steps, "The value was not over the min number");
        }

        [TestMethod]
        public async Task GuaranteedInterleavingTest()
        {
            int steps = 1000;
            int nodes = 2;
            string code =
                "function f(){" +
                    "for(let i=0;i<=" + steps + ";i++){" +
                    "hive.lock(\"sum\",function(){" +
                        "hive.set(\"sum\",hive.get(\"sum\")+1);" +
                    "});" +
                     "}" +
                   "}";
            var sessions = await chrome.CreateHiveSessions(nodes);
            await sessions[0].hiveSetValue("sum", 0);
            //propagation.
            await Task.WhenAll(sessions.Select(s => s.hiveWaitUntilValueEquals("sum", 0, 5000)));
            sessions.ForEach(async s => await s.EvalObject(code));

            await Task.WhenAll(sessions.Select(s => s.EvalObject("f()")));
            //check the final result?
            int total = await sessions[0].hiveWaitUntilGetValue<int>("sum", 100000);
            Assert.IsTrue(total == steps * nodes, "The value was not the expected number");
        }
    }
}