// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.PowerApps.PowerFxModel
{
    /// <summary>
    /// Class representing a Power FX TableValue for a Control or property
    /// </summary>
    public class ControlTableValue : CollectionTableValue<ControlTableRowSchema>
    {
        private readonly IPowerAppFunctions _powerAppFunctions;
        private readonly ILogger _logger;
        public ControlTableValue(RecordType recordType, IEnumerable<ControlTableRowSchema> source, IPowerAppFunctions powerAppFunctions, ILogger logger) : base(recordType, source)
        {
            _powerAppFunctions = powerAppFunctions;
            _logger = logger;
        }

        protected override DValue<RecordValue> Marshal(ControlTableRowSchema item)
        {
            var recordValue = new ControlRecordValue(item.RecordType, _powerAppFunctions, _logger, parentItemPath: item.ItemPath);
            return DValue<RecordValue>.Of(recordValue);
        }
    }
}
