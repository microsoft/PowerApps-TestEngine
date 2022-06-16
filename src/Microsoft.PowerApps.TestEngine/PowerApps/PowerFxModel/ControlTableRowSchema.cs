// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PowerApps.TestEngine.PowerApps.PowerFxModel
{
    /// <summary>
    /// Schema for a row in a table representing a control or it's property
    /// </summary>
    public class ControlTableRowSchema
    {
        public RecordType RecordType { get; set; }
        public ItemPath ItemPath { get; set; }

        public ControlTableRowSchema(RecordType recordType, ItemPath itemPath)
        {
            RecordType = recordType;
            ItemPath = itemPath;
        }
    }
}
