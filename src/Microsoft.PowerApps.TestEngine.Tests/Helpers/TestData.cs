// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerFx.Core.Public.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PowerApps.TestEngine.Tests.Helpers
{
    public static class TestData
    {
        public static JSPropertyModel[] CreateSampleJsPropertyModelList()
        {
            return new JSPropertyModel[]
            {
                new JSPropertyModel() { PropertyName = "Text", PropertyType = "String" },
                new JSPropertyModel() { PropertyName = "Color", PropertyType = "Color" },
                new JSPropertyModel() { PropertyName = "X", PropertyType = "Number" },
                new JSPropertyModel() { PropertyName = "Y", PropertyType = "Number" }
            };
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
            var arrayProperties = new Dictionary<string, FormulaType>()
            {
                { Guid.NewGuid().ToString(), FormulaType.String },
                { Guid.NewGuid().ToString(), FormulaType.Boolean },
                { Guid.NewGuid().ToString(), FormulaType.Number }
            };
        }
    }
}
