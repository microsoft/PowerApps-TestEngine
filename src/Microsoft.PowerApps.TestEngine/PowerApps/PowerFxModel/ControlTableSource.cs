// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.PowerApps.PowerFxModel
{
    /// <summary>
    /// Source of a table representing a control or property
    /// </summary>
    public class ControlTableSource : IReadOnlyList<ControlTableRowSchema>
    {
        private readonly IPowerAppFunctions _powerAppFunctions;
        private readonly ItemPath _itemPath;
        public RecordType RecordType { get; set; }

        public ControlTableSource(IPowerAppFunctions powerAppFunctions, ItemPath itemPath, RecordType recordType)
        {
            _powerAppFunctions = powerAppFunctions;
            _itemPath = itemPath;
            RecordType = recordType;
        }

        public ControlTableRowSchema this[int index] => new ControlTableRowSchema(
                                                        RecordType,
                                                        new ItemPath()
                                                        {
                                                            // Make a copy of item path so the index can be set correctly
                                                            ControlName = _itemPath.ControlName,
                                                            Index = index,
                                                            PropertyName = _itemPath.PropertyName,
                                                            ParentControl = _itemPath.ParentControl
                                                        });

        public int Count
        {
            get
            {
                // Always have to go fetch the count as it could dynamically change
                return _powerAppFunctions.GetItemCount(_itemPath);
            }
        }

        public IEnumerator<ControlTableRowSchema> GetEnumerator()
        {
            for (var i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
