// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Xml.Serialization;

namespace Microsoft.PowerApps.TestEngine.Reporting.Format
{
    public class ResultFile
    {
        [XmlAttribute(AttributeName = "path")]
        public string Path { get; set; }
    }
}
