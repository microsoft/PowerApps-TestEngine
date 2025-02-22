// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace testengine.provider.copilot.portal.services
{
    public interface IWorkerService
    {
        Task RunAsync(Func<Task> action);

        Task<bool> WaitUntilCompleteAsync(TimerCallback checkCondition, int timeout);
    }
}
