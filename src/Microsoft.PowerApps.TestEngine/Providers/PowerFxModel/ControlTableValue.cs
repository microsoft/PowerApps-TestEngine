// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.Providers.PowerFxModel
{
    /// <summary>
    /// Class representing a Power FX TableValue for a Control or property
    /// </summary>
    public class ControlTableValue : CollectionTableValue<ControlTableRowSchema>
    {
        public const string RowControlName = "TableRow";

        private readonly ITestWebProvider _testWebProvider;
        public ControlTableValue(RecordType recordType, IEnumerable<ControlTableRowSchema> source, ITestWebProvider testWebProvider) : base(recordType, source)
        {
            _testWebProvider = testWebProvider;
        }

        protected override DValue<RecordValue> Marshal(ControlTableRowSchema item)
        {
            ControlRecordValue recordValue = null;
            //special casing for mda revisit for objectmodel change
            if (string.Equals(_testWebProvider.Name ?? string.Empty, "mda", StringComparison.InvariantCultureIgnoreCase))
            {
                recordValue = new ControlRecordValue(item.RecordType, _testWebProvider, RowControlName, parentItemPath: item.ItemPath);
            }
            else
            {
                recordValue = new ControlRecordValue(item.RecordType, _testWebProvider, parentItemPath: item.ItemPath);
            }
            return DValue<RecordValue>.Of(recordValue);
        }
    }
}
