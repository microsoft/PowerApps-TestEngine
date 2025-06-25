// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Concurrent;

namespace Microsoft.PowerApps.TestEngine.Providers
{
    /// <summary>
    /// Interface for providers that can send and receive messages
    /// </summary>
    public interface IMessageProvider
    {
        /// <summary>
        /// Queue of messages from the provider
        /// </summary>
        ConcurrentQueue<string> Messages { get; }

        /// <summary>
        /// Current conversation ID
        /// </summary>
        string? ConversationId { get; set; }

        /// <summary>
        /// Worker service for handling messages
        /// </summary>
        IWorkerService MessageWorker { get; set; }

        /// <summary>
        /// Worker service for handling actions
        /// </summary>
        IWorkerService ActionWorker { get; set; }

        /// <summary>
        /// Get new messages from the provider
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task GetNewMessages();
    }
}
