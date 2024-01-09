using Serilog;
using System.ComponentModel;

namespace LocalHangFire.Services
{
    public class SimpleJobService
    {
        public SimpleJobService()
        {
        }

        [DisplayName("排程作業: {0} - {1}")]
        public void OneTimeJobExecute(string userId, string jobName)
        {
            Log.Logger.Information("One Time Job Execute!");
        }

        [DisplayName("排程作業: {0} - {1}")]
        public void DelayJobExecute(string userId, string jobName)
        {
            Log.Logger.Information("Delay Job Execute!");
        }

        [DisplayName("排程作業: {0} - {1}")]
        public void SchedulerJobExecute(string userId, string jobName)
        {
            Log.Logger.Information("Scheduler Job Execute!");
        }
    }
}
