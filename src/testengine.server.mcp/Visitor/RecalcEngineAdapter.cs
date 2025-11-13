// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestEngine.PowerFx.Functions;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.MCP.Visitor
{
    /// <summary>
    /// Adapter class that wraps the Microsoft.PowerFx.RecalcEngine to implement the IRecalcEngine interface.
    /// Provides testable access to PowerFx functionality.
    /// </summary>
    public class RecalcEngineAdapter : IRecalcEngine
    {
        private readonly RecalcEngine _engine;

        /// <summary>
        /// Creates a new RecalcEngineAdapter instance.
        /// </summary>
        /// <param name="engine">The PowerFx RecalcEngine instance to adapt</param>
        /// <exception cref="ArgumentNullException">Thrown when the engine parameter is null</exception>
        public RecalcEngineAdapter(RecalcEngine engine, Extensions.Logging.ILogger logger)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));

            engine.Config.AddFunction(new IsMatchFunction(logger));
        }

        /// <summary>
        /// Evaluates a PowerFx expression using the specified options.
        /// </summary>
        /// <param name="expression">The PowerFx expression to evaluate</param>
        /// <param name="options">Parser options for expression evaluation</param>
        /// <returns>The result of the expression evaluation</returns>
        public FormulaValue Eval(string expression, ParserOptions options)
        {
            return _engine.Eval(expression, options: options);
        }

        /// <summary>
        /// Parses a PowerFx expression using the specified options.
        /// </summary>
        /// <param name="expression">The PowerFx expression to parse</param>
        /// <param name="options">Parser options for expression parsing</param>
        /// <returns>The parse result containing the expression syntax tree</returns>
        public ParseResult Parse(string expression, ParserOptions options)
        {
            return _engine.Parse(expression, options);
        }

        /// <summary>
        /// Updates a variable value in the engine's context.
        /// </summary>
        /// <param name="name">The name of the variable to update</param>
        /// <param name="value">The new value for the variable</param>
        public void UpdateVariable(string name, FormulaValue value)
        {
            _engine.UpdateVariable(name, value);
        }
    }
}
