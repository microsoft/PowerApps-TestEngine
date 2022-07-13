// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerFx.Types;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PowerApps.TestEngine.PowerApps.PowerFxModel
{
    /// <summary>
    /// This is a Power FX RecordValue created to represent a control or a control property
    /// </summary>
    public class ControlRecordValue : RecordValue
    {
        private readonly IPowerAppFunctions _powerAppFunctions;
        private readonly string _name;
        private readonly ItemPath _parentItemPath;

        /// <summary>
        /// Creates a ControlRecordValue
        /// </summary>
        /// <param name="type">Record type for the control record value</param>
        /// <param name="powerAppFunctions">Power App functions so that the property values can be fetched</param>
        /// <param name="name">Name of the control</param>
        /// <param name="parentItemPath">Path to the parent control</param>
        public ControlRecordValue(RecordType type, IPowerAppFunctions powerAppFunctions, string name = null, ItemPath parentItemPath = null) : base(type)
        {
            _powerAppFunctions = powerAppFunctions;
            _parentItemPath = parentItemPath;
            _name = name;
        }

        /// <summary>
        /// Gets the name of the control
        /// </summary>
        public string Name {
            get {
                return _name;
            }
        }

        /// <summary>
        /// Gets the path to the control that can be used by the javascript
        /// </summary>
        /// <param name="propertyName">Property name. Optional</param>
        /// <returns>Path to control</returns>
        public ItemPath GetItemPath(string propertyName = null)
        {
            return new ItemPath()
            {
                ControlName = _name,
                PropertyName = propertyName,
                ParentControl = _parentItemPath
            };
        }

        /// <summary>
        /// Gets a field from the control.
        /// </summary>
        /// <param name="fieldType">Type of the field</param>
        /// <param name="fieldName">Name of the field</param>
        /// <param name="result">Value of the field</param>
        /// <returns>True if able to get the field value</returns>
        protected override bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
        {
            if (fieldType is TableType)
            {
                // This would be if we were referencing a property that could be indexed. Eg. Gallery1.AllItems (fieldName = AllItems)
                var tableType = fieldType as TableType;
                var recordType = tableType.ToRecord();
                // Create indexable table source
                var tableSource = new ControlTableSource(_powerAppFunctions, GetItemPath(fieldName), recordType);
                var table = new ControlTableValue(recordType, tableSource, _powerAppFunctions);
                result = table;
                return true;
            }
            else if (fieldType is RecordType)
            {
                var recordType = fieldType as RecordType;
                if (string.IsNullOrEmpty(_name))
                {
                    // We reach here if we are referencing a child item in a Gallery. Eg. Index(Gallery1.AllItems).Label1 (fieldName = Label1)
                    result = new ControlRecordValue(recordType, _powerAppFunctions, fieldName, _parentItemPath);
                    return true;
                }
                else
                {
                    // We reach here if we are referencing a child item in a component. Eg. Component1.Label1 (fieldName = Label1)
                    result = new ControlRecordValue(recordType, _powerAppFunctions, fieldName, GetItemPath());
                    return true;
                }
            }
            else
            {
                // We reach here if we are referencing a terminating property of a control, Eg. Label1.Text (fieldName = Text)
                var itemPath = GetItemPath(fieldName);

                var propertyValueJson = _powerAppFunctions.GetPropertyValueFromControl<string>(itemPath);
                var jsPropertyValueModel = JsonConvert.DeserializeObject<JSPropertyValueModel>(propertyValueJson);

                if (jsPropertyValueModel != null)
                {
                    if (fieldType is NumberType)
                    {
                        result = NumberValue.New(double.Parse(jsPropertyValueModel.PropertyValue));
                        return true;
                    }
                    else if (fieldType is BooleanType)
                    {
                        result = BooleanValue.New(bool.Parse(jsPropertyValueModel.PropertyValue));
                        return true;
                    }
                    else if (fieldType is DateTimeType)
                    {
                        result = DateTimeValue.New(DateTime.Parse(jsPropertyValueModel.PropertyValue));
                        return true;
                    }

                    result = New(jsPropertyValueModel.PropertyValue);
                    return true;
                }
            }

            result = null;
            return false;
        }
    }
}
