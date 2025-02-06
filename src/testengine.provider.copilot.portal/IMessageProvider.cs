using System;
using System.Collections.Concurrent;

namespace Microsoft.PowerApps.TestEngine.Providers
{
    public interface IMessageProvider
    {
        Task GetNewMessages();

        /// <summary>
        /// Json messages observed as part of the test session
        /// </summary>
        ConcurrentQueue<string> Messages { get;  }
    }
}
