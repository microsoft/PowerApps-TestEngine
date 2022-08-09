﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Xml.Serialization;

namespace Microsoft.PowerApps.TestEngine.Reporting.Format
{
    public class TestLists
    {
        [XmlElement(ElementName = "TestList")]
        public List<TestList>? TestList { get; set; }
    }
}
