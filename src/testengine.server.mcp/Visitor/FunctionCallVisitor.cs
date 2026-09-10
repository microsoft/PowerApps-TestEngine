// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerApps.TestEngine.MCP.Visitor
{
    /// <summary>
    /// Visitor class that traverses the PowerFx syntax tree to find function calls.
    /// Implements the IdentityTexlVisitor from the PowerFx library.
    /// </summary>
    public class FunctionCallVisitor : IdentityTexlVisitor
    {
        private readonly HashSet<string> _foundFunctions = new HashSet<string>();

        /// <summary>
        /// Gets the collection of function names discovered during traversal.
        /// </summary>
        public IReadOnlyCollection<string> FoundFunctions => _foundFunctions;

        /// <summary>
        /// Called when a function call node is visited in the syntax tree.
        /// </summary>
        /// <param name="node">The function call node being visited</param>
        /// <remarks>
        /// This method extracts the function name from the call node and adds it to the collection.
        /// It then continues traversal of the syntax tree to find any nested function calls.
        /// </remarks>
        public override bool PreVisit(CallNode node)
        {
            // Add the function name to our collection
            _foundFunctions.Add(node.Head.Name.Value);

            // Continue traversing the AST
            return true;
        }
    }
}
