using Microsoft.Extensions.Logging;

namespace testengine.provider.copilot.portal.services
{
    public class SingleThreadedWorkerService : IWorkerService
    {
        public async Task<bool> WaitUntilCompleteAsync(TimerCallback checkCondition, int timeout)
        {
            var startTime = DateTime.Now;

            var state = new TimerState();

            while (DateTime.Now.Subtract(startTime).TotalSeconds < timeout)
            {
                checkCondition(state);

                if (state.Tcs.Task.IsCompleted)
                {
                    return state.Tcs.Task.Result;
                }

                Thread.Sleep(1000); // Delay for 1 second before checking again
            }

            return false;
        }
    }
}
