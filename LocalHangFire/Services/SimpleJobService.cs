using System.ComponentModel;

namespace LocalHangFire.Services
{
    public class SimpleJobService
    {
        private readonly ILogger<SimpleJobService> _logger;

        public SimpleJobService(
           ILogger<SimpleJobService> logger)
        {
            _logger = logger;
        }

        [DisplayName("排程作業: {0} - {1}")]
        public void OneTimeJobExecute(string userId, string jobName)
        {
            _logger.LogInformation("One Time Job Execute!");
        }

        [DisplayName("排程作業: {0} - {1}")]
        public void DelayJobExecute(string userId, string jobName)
        {
            _logger.LogInformation("Delay Job Execute!");
        }

        [DisplayName("排程作業: {0} - {1}")]
        public void SchedulerJobExecute(string userId, string jobName)
        {
            _logger.LogInformation("Scheduler Job Execute!");
        }
    }
}
