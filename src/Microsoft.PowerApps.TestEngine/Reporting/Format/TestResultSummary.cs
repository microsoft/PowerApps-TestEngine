// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Xml.Serialization;

namespace Microsoft.PowerApps.TestEngine.Reporting.Format
{
    public class TestResultSummary
    {
        [XmlAttribute(AttributeName = "outcome")]
        public string Outcome { get; set; }
        [XmlElement(ElementName = "Counters")]
        public TestCounters Counters { get; set; }
        [XmlElement(ElementName = "Output")]
        public TestOutput Output { get; set; }
    }
}
