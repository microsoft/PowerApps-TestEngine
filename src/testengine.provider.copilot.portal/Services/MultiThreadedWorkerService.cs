// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;

namespace testengine.provider.copilot.portal.services
{
    public class MultiThreadedWorkerService : IWorkerService
    {
        private readonly ILogger? _logger;
        private TimerState _timerState;

        public MultiThreadedWorkerService(ILogger logger)
        {
            _logger = logger;
            _timerState = new TimerState();
        }

        public Task RunAsync(Func<Task> action)
        {
            return Task.Factory.StartNew(async () => { await action(); });
        }

        public async Task<bool> WaitUntilCompleteAsync(TimerCallback checkcondition, int timeout)
        {
            _timerState = new TimerState();
            
            var startTime = DateTime.Now;
            var task = _timerState.Tcs.Task;

            var timeoutTask = Task.Run(async () =>
            {
                while (DateTime.Now.Subtract(startTime).TotalSeconds < timeout)
                {
                    checkcondition(_timerState);
                    if (task.IsCompleted)
                    {
                        break;
                    }
                    await Task.Delay(1000);
                }
            });

            if (await Task.WhenAny(task, timeoutTask) == task)
            {
                _logger?.LogInformation("Condition met");
                return true;
            }
            else
            {
                _logger?.LogInformation("Timeout reached");
                return false;
            }
        }

    }
}
