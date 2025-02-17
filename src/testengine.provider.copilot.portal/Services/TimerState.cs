// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace testengine.provider.copilot.portal.services
{
    public class TimerState
    {
        public TaskCompletionSource<bool> Tcs { get; set; } = new TaskCompletionSource<bool>();
    }
}
