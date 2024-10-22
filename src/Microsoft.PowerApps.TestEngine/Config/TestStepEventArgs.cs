using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.Config
{
    /// <summary>
    /// Represents the event arguments for a test step event.
    /// </summary>
    public class TestStepEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the name of the test step.
        /// </summary>
        public string TestStep { get; set; }

        /// <summary>
        /// Gets or sets the result of the test step.
        /// </summary>
        public FormulaValue Result { get; set; }

        /// <summary>
        /// Gets or sets the step number of the test step.
        /// </summary>
        public int? StepNumber { get; set; }

        /// <summary>
        /// Gets or sets the recalculation engine used for the test step.
        /// </summary>
        public RecalcEngine Engine { get; set; }
    }
}
