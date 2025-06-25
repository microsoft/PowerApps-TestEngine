// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerApps.TestEngine.Providers
{
    /// <summary>
    /// Interface for worker services that handle background processing
    /// </summary>
    public interface IWorkerService
    {
        /// <summary>
        /// Start the worker service
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task StartAsync();

        /// <summary>
        /// Stop the worker service
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task StopAsync();

        /// <summary>
        /// Gets whether the worker service is running
        /// </summary>
        bool IsRunning { get; }
    }
}
