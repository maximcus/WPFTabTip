using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WPFTabTip
{
    public static class PoolingTimer
    {
        private static Timer timer;

        public static void Start(Func<bool> StopTimer, TimeSpan dueTime, TimeSpan period)
        {
            if (timer == null)
            {
                timer = new Timer(
                    (obj) =>
                    {
                        if (StopTimer())
                            Dispose();
                    }, 
                state: null, 
                dueTime: (int) dueTime.TotalMilliseconds, 
                period: (int) period.TotalMilliseconds);
            }
        }

        private static void Dispose()
        {
            try
            {
                timer.Dispose();
            }
            catch (Exception)
            {
                // ignore
            }
            finally
            {
                timer = null;
            }
        }
    }
}
