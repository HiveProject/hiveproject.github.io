using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace HiveTests
{
    public static class asyncSpinlock
    {
        private const int getMsDelay = 10;

        public static async Task<bool> WaitUntil(Func<Task<bool>> f, int timeout = 1000)
        {
            //TODO(Tera): use stopwatch maybe?
            DateTime end = DateTime.Now.AddMilliseconds(timeout);
            while (end >= DateTime.Now)
            {
                if (await f()) return true;
                await Task.Delay(getMsDelay);
            }
            return false;
        }
    }
}
