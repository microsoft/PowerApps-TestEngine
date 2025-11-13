// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.MCP.Visitor
{
    /// <summary>
    /// Interface for the RecalcEngine to enable testing and dependency injection.
    /// </summary>
    /// <remarks>
    /// This interface abstracts the PowerFx RecalcEngine functionality to enable
    /// unit testing with mock implementations for formula evaluation.
    /// </remarks>
    public interface IRecalcEngine
    {
        /// <summary>
        /// Evaluates a PowerFx expression using the specified options.
        /// </summary>
        /// <param name="expression">The PowerFx expression to evaluate</param>
        /// <param name="options">Parser options for expression evaluation</param>
        /// <returns>The result of the expression evaluation</returns>
        FormulaValue Eval(string expression, ParserOptions options);

        /// <summary>
        /// Updates a variable value in the engine's context.
        /// </summary>
        /// <param name="name">The name of the variable to update</param>
        /// <param name="value">The new value for the variable</param>
        void UpdateVariable(string name, FormulaValue value);

        /// <summary>
        /// Parses a PowerFx expression using the specified options.
        /// </summary>
        /// <param name="expression">The PowerFx expression to parse</param>
        /// <param name="options">Parser options for expression parsing</param>
        /// <returns>The parse result containing the expression syntax tree</returns>
        ParseResult Parse(string expression, ParserOptions options);
    }
}
