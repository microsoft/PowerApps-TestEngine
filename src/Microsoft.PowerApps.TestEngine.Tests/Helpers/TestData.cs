// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.Tests.Helpers
{
    public static class TestData
    {
        public static JSPropertyModel[] CreateSampleJsPropertyModelList(JSPropertyModel[] additionalProperties = null)
        {
            var properties = new List<JSPropertyModel>() {
                new JSPropertyModel() { PropertyName = "Text", PropertyType = "s" },
                new JSPropertyModel() { PropertyName = "Color", PropertyType = "c" },
                new JSPropertyModel() { PropertyName = "X", PropertyType = "n" },
                new JSPropertyModel() { PropertyName = "Y", PropertyType = "n" }
            };
            if (additionalProperties != null)
            {
                foreach (var additionalProperty in additionalProperties)
                {
                    properties.Add(additionalProperty);
                }
            }
            return properties.ToArray();
        }

        public static Dictionary<string, FormulaType> CreateExpectedFormulaTypesForSampleJsPropertyModelList()
        {
            var dict = new Dictionary<string, FormulaType>();
            dict.Add("Text", FormulaType.String);
            dict.Add("Color", FormulaType.Color);
            dict.Add("X", FormulaType.Number);
            dict.Add("Y", FormulaType.Number);
            return dict;
        }

        public static Dictionary<string, FormulaType> CreateSamplePropertiesDictionary()
        {
            return new Dictionary<string, FormulaType>() { { "Text", FormulaType.String } };
        }

        public static Dictionary<string, FormulaType> CreateRandomPropertiesDictionary()
        {
            return new Dictionary<string, FormulaType>()
            {
                { Guid.NewGuid().ToString(), FormulaType.String },
                { Guid.NewGuid().ToString(), FormulaType.Boolean },
                { Guid.NewGuid().ToString(), FormulaType.Number }
            };
        }
    }
}
