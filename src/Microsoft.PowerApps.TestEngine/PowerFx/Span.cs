// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerApps.TestEngine.PowerFx
{
    /// <summary>
    /// Represents a span with start and end
    /// Note: PowerFx Span object model is internal so we need our own span object model
    /// </summary>
    internal class Span
    {
        public int Start { get; }

        public int End { get; }

        public Span(int min, int lim)
        {
            Start = min;
            End = lim;
        }

        public Span(Span span)
        {
            Start = span.Start;
            End = span.End;
        }
    }
}
