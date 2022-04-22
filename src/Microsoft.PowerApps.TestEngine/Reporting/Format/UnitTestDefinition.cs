// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Xml.Serialization;

namespace Microsoft.PowerApps.TestEngine.Reporting.Format
{
    public class UnitTestDefinition
    {
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }
        [XmlAttribute(AttributeName = "storage")]
        public string Storage { get; set; }
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
        [XmlElement(ElementName = "Execution")]
        public TestExecution Execution { get; set; }
        [XmlElement(ElementName = "TestMethod")]
        public TestMethod Method { get; set; }
    }
}
