// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Xml.Serialization;

namespace Microsoft.PowerApps.TestEngine.Reporting.Format
{
    public class UnitTestResult
    {
        [XmlAttribute(AttributeName = "executionId")]
        public string ExecutionId { get; set; }
        [XmlAttribute(AttributeName = "testId")]
        public string TestId { get; set; }
        [XmlAttribute(AttributeName = "testName")]
        public string TestName { get; set; }
        [XmlAttribute(AttributeName = "computerName")]
        public string ComputerName { get; set; }
        [XmlAttribute(AttributeName = "duration")]
        public string Duration { get; set; }
        [XmlAttribute(AttributeName = "startTime")]
        public DateTime StartTime { get; set; }
        [XmlAttribute(AttributeName = "endTime")]
        public DateTime EndTime { get; set; }
        [XmlAttribute(AttributeName = "testType")]
        public string TestType { get; set; }
        [XmlAttribute(AttributeName = "outcome")]
        public string Outcome { get; set; }
        [XmlAttribute(AttributeName = "testListId")]
        public string TestListId { get; set; }
        [XmlAttribute(AttributeName = "relativeResultsDirectory")]
        public string RelativeResultsDirectory { get; set; }
        [XmlElement(ElementName = "Output")]
        public TestOutput Output { get; set; }
        [XmlElement(ElementName = "ResultFiles")]
        public TestResultFiles ResultFiles { get; set; }
    }
}
