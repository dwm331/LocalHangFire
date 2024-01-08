using Hangfire;
using LocalHangFire.Services;
using Microsoft.AspNetCore.Mvc;

namespace LocalHangFire.Controllers
{
    public class HangfireSimpleController : ControllerBase
    {
        private readonly SimpleJobService _simpleJobService;

        public HangfireSimpleController(SimpleJobService simpleJobService)
        {
            _simpleJobService = simpleJobService;
        }

        /// <summary>
        /// 一次性的任務
        /// </summary>
        /// <returns></returns>
        [HttpGet("OneTimeJob")]
        public IActionResult OneTime()
        {
            string userId = $"admin 建立於 {DateTime.Now:MM-dd HH:mm:ss}";
            string jobName = "一次性的任務";
            BackgroundJob.Enqueue<SimpleJobService>(x => x.OneTimeJobExecute(userId, jobName));
            return Ok();
        }

        /// <summary>
        /// 延遲性任務，設定特定時間執行
        /// </summary>
        /// <returns></returns>
        [HttpGet("DelayJob")]
        public IActionResult DelayJob()
        {
            string userId = $"admin 建立於 {DateTime.Now:MM-dd HH:mm:ss}";
            string jobName = "延遲性任務";
            BackgroundJob.Schedule<SimpleJobService>(x => x.DelayJobExecute(userId, jobName), TimeSpan.FromSeconds(30));
            return Ok();
        }

        /// <summary>
        /// 週期性任務
        /// </summary>
        /// <returns></returns>
        [HttpGet("SchedulerJob")]
        public IActionResult Scheduler()
        {
            // MyDailyScheduler 自訂識別
            string userId = $"admin 建立於 {DateTime.Now:MM-dd HH:mm:ss}";
            string jobName = "週期性任務";
            RecurringJob.AddOrUpdate("MyDailySchedulerJob", () => _simpleJobService.SchedulerJobExecute(userId, jobName), Cron.Daily);
            return Ok();
        }
    }
}
