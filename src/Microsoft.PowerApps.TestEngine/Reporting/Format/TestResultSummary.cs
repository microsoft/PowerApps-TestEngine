// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Xml.Serialization;

namespace Microsoft.PowerApps.TestEngine.Reporting.Format
{
    public class TestResultSummary
    {
        [XmlAttribute(AttributeName = "outcome")]
        public string? Outcome { get; set; }
        [XmlElement(ElementName = "Counters")]
        public TestCounters? Counters { get; set; }
        [XmlElement(ElementName = "Output")]
<<<<<<< HEAD
        public TestOutput? Output {get; set; }
=======
        public TestOutput Output { get; set; }
>>>>>>> 0e8d7934241fda6063d76295e6538e84fc048280
    }
}
