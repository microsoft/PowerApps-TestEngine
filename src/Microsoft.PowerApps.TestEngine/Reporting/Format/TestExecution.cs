// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Xml.Serialization;

namespace Microsoft.PowerApps.TestEngine.Reporting.Format
{
    public class TestExecution
    {
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
    }
}
