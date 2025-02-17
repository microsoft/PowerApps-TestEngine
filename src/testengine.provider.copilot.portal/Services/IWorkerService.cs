// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace testengine.provider.copilot.portal.services
{
    public interface IWorkerService
    {
        Task<bool> WaitUntilCompleteAsync(TimerCallback checkCondition, int timeout);
    }
}
