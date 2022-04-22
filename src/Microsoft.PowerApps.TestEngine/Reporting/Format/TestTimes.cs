// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Xml.Serialization;

namespace Microsoft.PowerApps.TestEngine.Reporting.Format
{
    public class TestTimes
    {
        [XmlAttribute(AttributeName = "creation")]
        public DateTime Creation { get; set; }
        [XmlAttribute(AttributeName = "queuing")]
        public DateTime Queuing { get; set; }
        [XmlAttribute(AttributeName = "start")]
        public DateTime Start { get; set; }
        [XmlAttribute(AttributeName = "finish")]
        public DateTime Finish { get; set; }
    }
}
