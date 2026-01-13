// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Moq;
using Moq.Language.Flow;

namespace Microsoft.PowerApps.TestEngine.MCP.Tests.PowerFx
{
    /// <summary>
    /// Helper class for capturing arguments in Moq verifications.
    /// This addresses issues with expression trees when using It.Is with optional parameters.
    /// </summary>
    public static class Capture
    {
        /// <summary>
        /// Captures the actual value passed to a mocked method in a collection.
        /// </summary>
        /// <typeparam name="T">The type of value to capture.</typeparam>
        /// <param name="collection">The collection to add the captured value to.</param>
        /// <returns>A matcher that will capture the actual value.</returns>
        public static T In<T>(ICollection<T> collection)
        {
            return It.IsAny<T>().Capture(collection);
        }
    }

    /// <summary>
    /// Extension methods for Moq to help with testing.
    /// </summary>
    public static class MoqExtensions
    {
        /// <summary>
        /// Captures the value and adds it to the specified collection.
        /// </summary>
        /// <typeparam name="T">The type of value to capture.</typeparam>
        /// <param name="value">The value to capture.</param>
        /// <param name="collection">The collection to add the captured value to.</param>
        /// <returns>The original value.</returns>
        public static T Capture<T>(this T value, ICollection<T> collection)
        {
            collection.Add(value);
            return value;
        }
    }
}
