using System.Collections.Concurrent;
using Application.Services.Interfaces;

namespace Application.Services.Implementations
{
    public class BackgroundTaskService : IBackgroundTaskService
    {
        private readonly ILoggerService _logger;
        private readonly ConcurrentDictionary<string, Task> _runningTasks = new();

        public BackgroundTaskService(ILoggerService logger)
        {
            _logger = logger;
        }
        public void RunInBackground(string taskName, Func<Task> action)
        {
            var task = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation($"[BackgroundTask] Starting: {taskName}");
                    await action();
                    _logger.LogInformation($"[BackgroundTask] Completed: {taskName}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[BackgroundTask] Failed: {taskName} - {ex.Message}");
                }
                finally
                {
                    // Auto cleanup
                    _runningTasks.TryRemove(taskName, out _);
                }
            });

            // Store task reference (optional, for cleanup)
            _runningTasks.TryAdd(taskName, task);
        }
    }
}
