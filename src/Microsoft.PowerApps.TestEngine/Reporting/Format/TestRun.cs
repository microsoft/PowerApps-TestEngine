// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Xml.Serialization;

namespace Microsoft.PowerApps.TestEngine.Reporting.Format
{
    [XmlRoot(Namespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010", IsNullable = true)]
    public class TestRun
    {
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }
        [XmlAttribute(AttributeName = "runUser")]
        public string RunUser { get; set; }
        [XmlElement(ElementName = "Times")]
        public TestTimes Times { get; set; }
        [XmlElement(ElementName = "Results")]
        public TestResults Results { get; set; }
        [XmlElement(ElementName = "TestDefinitions")]
        public TestDefinitions Definitions { get; set; }
        [XmlElement(ElementName = "TestEntries")]
        public TestEntries TestEntries { get; set; }
        [XmlElement(ElementName = "TestLists")]
        public TestLists TestLists { get; set; }
        [XmlElement(ElementName = "ResultSummary")]
        public TestResultSummary ResultSummary { get; set; }
    }
}
