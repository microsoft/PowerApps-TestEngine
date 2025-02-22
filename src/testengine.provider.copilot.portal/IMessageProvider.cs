using System;
using System.Collections.Concurrent;
using testengine.provider.copilot.portal.services;

namespace Microsoft.PowerApps.TestEngine.Providers
{
    public interface IMessageProvider
    {
        public string? ConversationId { get; set; }

        public IWorkerService MessageWorker { get; set; }

        public IWorkerService ActionWorker { get; set; }

        Task GetNewMessages();

        /// <summary>
        /// Json messages observed as part of the test session
        /// </summary>
        ConcurrentQueue<string> Messages { get;  }
    }
}
