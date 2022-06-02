// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;
using Newtonsoft.Json;

namespace Microsoft.PowerApps.TestEngine.PowerApps
{
    /// <summary>
    /// IUntypedObject model of a Power App Control
    /// </summary>
    public class PowerAppControlModel : IUntypedObject
    {
        public IUntypedObject this[int index] => IsArrayObject() ? CreateControlAtIndex(index) : throw new NotImplementedException();

        public FormulaType Type => IsArrayObject() ? ExternalType.ArrayType : ExternalType.ObjectType;

        public string Name { get; set; }
        public Dictionary<string, FormulaType> Properties { get; set; }
        public int? ItemCount { get; set; }
        public int? SelectedIndex { get; set; }
        public List<PowerAppControlModel> ChildControls { get; set; }

        public PowerAppControlModel? ParentControl { get; set; }

        private IPowerAppFunctions PowerAppFunctions { get; set; }

        public PowerAppControlModel(string name, Dictionary<string, FormulaType> properties, IPowerAppFunctions powerAppFunctions)
        {
            PowerAppFunctions = powerAppFunctions;
            Name = name;
            Properties = properties;
            ChildControls = new List<PowerAppControlModel>();
        }

        public PowerAppControlModel(PowerAppControlModel model, int selectedIndex)
        {
            Name = model.Name;
            PowerAppFunctions = model.PowerAppFunctions;
            Properties = model.Properties;
            ChildControls = new List<PowerAppControlModel>();
            ItemCount = model.ItemCount;
            ParentControl = model.ParentControl;
            SelectedIndex = selectedIndex;
        }

        private bool IsArrayObject()
        {
            return ItemCount.HasValue && !SelectedIndex.HasValue;
        }

        private PowerAppControlModel CreateControlAtIndex(int selectedIndex)
        {
            if (selectedIndex >= ItemCount)
            {
                throw new IndexOutOfRangeException();
            }

            var control = new PowerAppControlModel(this, selectedIndex);
            foreach(var childControl in ChildControls)
            {
                // Need to make a copy of each child control for the selected index
                var newChildControl = new PowerAppControlModel(childControl.Name, childControl.Properties, PowerAppFunctions);
                newChildControl.ItemCount = childControl.ItemCount;
                newChildControl.ChildControls = new List<PowerAppControlModel>(childControl.ChildControls);
                control.AddChildControl(newChildControl);
            }
            return control;
        }

        public void AddChildControl(PowerAppControlModel childControl)
        {
            childControl.ParentControl = this;
            ChildControls.Add(childControl);
        }

        public int GetArrayLength()
        {
            if (ItemCount.HasValue)
            {
                return ItemCount.Value;
            }

            throw new NotImplementedException();
        }

        public bool GetBoolean()
        {
            throw new NotImplementedException();
        }

        public double GetDouble()
        {
            throw new NotImplementedException();
        }

        public string GetString()
        {
            throw new NotImplementedException();
        }

        public ItemPath CreateItemPath(ItemPath? childItemPath = null, string? propertyName = null)
        {
            var itemPath = new ItemPath
            {
                ControlName = Name,
                Index = SelectedIndex,
                ChildControl = childItemPath,
                PropertyName = propertyName
            };

            if (ParentControl != null)
            {
                var parentItemPath = ParentControl.CreateItemPath(itemPath);
                return parentItemPath;
            }

            return itemPath;
        }

        public bool TryGetProperty(string value, out IUntypedObject result)
        {
            if (Properties.Keys.Contains(value))
            {
                var itemPath = CreateItemPath(propertyName: value);
                var getProperty = PowerAppFunctions.GetPropertyValueFromControlAsync<string>(itemPath).GetAwaiter();

                // TODO: implement timeout
                while (!getProperty.IsCompleted)
                {
                    Thread.Sleep(500);
                }

                string propertyValueJson = getProperty.GetResult();
                var jsPropertyValueModel = JsonConvert.DeserializeObject<JSPropertyValueModel>(propertyValueJson);

                if (jsPropertyValueModel != null)
                {
                    result = new PowerAppControlPropertyModel(value, jsPropertyValueModel.PropertyValue, Properties[value]);
                    return true;
                }

            }

            if (ItemCount.HasValue && !SelectedIndex.HasValue)
            {
                // This is an array element but the index hasn't been selected
                // Not able to refernce this
                result = null;
                return false;
            }

            // If it isn't a property value, then see if it's a child control
            var childControl = ChildControls.FirstOrDefault(x => x.Name == value);

            if (childControl != null)
            {
                result = childControl;
                return true;
            }

            result = null;
            return false;
        }
    }
}
