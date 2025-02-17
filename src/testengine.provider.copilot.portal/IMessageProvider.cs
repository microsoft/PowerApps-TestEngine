using System;
using System.Collections.Concurrent;

namespace Microsoft.PowerApps.TestEngine.Providers
{
    public interface IMessageProvider
    {
        public string? ConversationId { get; set; }

        Task GetNewMessages();

        /// <summary>
        /// Json messages observed as part of the test session
        /// </summary>
        ConcurrentQueue<string> Messages { get;  }
    }
}
