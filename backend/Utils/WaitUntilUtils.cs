// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
//
// Author: Abel Dieterich

using System.Linq.Expressions;
using System.Reflection;
using KnowledgeBank.Data;

namespace KnowledgeBank.Utils 
{
    /// <summary>
    /// Utility class for waiting until a specific time of day.
    /// </summary>
    public static class WaitUntilUtils
    {
        /// <summary>
        /// Asynchronously waits until the next occurrence of the specified daily time.
        /// For example, if runTime is 03:00, it waits until 3 AM today or tomorrow if that time already passed.
        /// </summary>
        /// <param name="runTime">The daily time of day to wait until (e.g., 3 AM is TimeSpan.FromHours(3)).</param>
        /// <param name="stoppingToken">A cancellation token to stop waiting early if requested.</param>
        /// <returns>A task that completes when the specified time is reached or the operation is cancelled.</returns>
        public static async Task WaitUntilTime(TimeSpan runTime, CancellationToken stoppingToken)
        {
            TimeSpan normalizedRunTime = runTime.Add(TimeSpan.FromDays(runTime.TotalDays % 1));
            DateTime now = DateTime.Now;
            DateTime nextRun = now.Date.Add(runTime);

            if (now > nextRun)
                nextRun = nextRun.AddDays(1);

            TimeSpan delay = nextRun - now;
            await Task.Delay(delay, stoppingToken);
        }
    }
}