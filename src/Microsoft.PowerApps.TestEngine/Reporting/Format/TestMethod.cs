// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Xml.Serialization;

namespace Microsoft.PowerApps.TestEngine.Reporting.Format
{
    public class TestMethod
    {
        [XmlAttribute(AttributeName = "codeBase")]
        public string CodeBase { get; set; }
        [XmlAttribute(AttributeName = "adapterTypeName")]
        public string AdapterTypeName { get; set; }
        [XmlAttribute(AttributeName = "className")]
        public string ClassName { get; set; }
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }
    }
}
