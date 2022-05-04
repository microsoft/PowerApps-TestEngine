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
        public IUntypedObject this[int index] => throw new NotImplementedException();

        public FormulaType Type => ExternalType.ObjectType;

        public string Name { get; set; }
        public List<string> Properties { get; set; }
        private IPowerAppFunctions PowerAppFunctions { get; set; }

        public PowerAppControlModel(string name, List<string> properties, IPowerAppFunctions powerAppFunctions)
        {
            PowerAppFunctions = powerAppFunctions;
            Name = name;
            Properties = properties;
        }

        public int GetArrayLength()
        {
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

        public bool TryGetProperty(string value, out IUntypedObject result)
        {
            if (Properties.Contains(value))
            {
                var getProperty = PowerAppFunctions.GetPropertyValueFromControlAsync<string>(Name, value, null, null).GetAwaiter();

                // TODO: implement timeout
                while (!getProperty.IsCompleted)
                {
                    Thread.Sleep(500);
                }

                string propertyValueJson = getProperty.GetResult();
                var jsPropertyValueModel = JsonConvert.DeserializeObject<JSPropertyValueModel>(propertyValueJson);

                if (jsPropertyValueModel != null)
                {
                    result = new PowerAppControlPropertyModel(value, jsPropertyValueModel.PropertyValue);
                    return true;
                }

            }
            result = null;
            return false;
        }
    }
}
