// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerFx.Core.Public.Types;
using System;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerApps
{
    public class PowerAppControlPropertyModelTests
    {
        [Fact]
        public void PowerAppControlPropertyModelForStringTest()
        {
            var name = "Text";
            var value = "Hello";
            var model = new PowerAppControlPropertyModel(name, value);
            Assert.Equal(name, model.Name);
            Assert.Equal(value, model.Value);
            Assert.Equal(FormulaType.String, model.Type);
            Assert.Throws<NotImplementedException>(() => model.GetArrayLength());
            Assert.Equal(value, model.GetString());
            Assert.Throws<NotImplementedException>(() => model[4]);
            Assert.Throws<NotImplementedException>(() => model.TryGetProperty("property", out var result));
            Assert.Throws<FormatException>(() => model.GetBoolean());
            Assert.Throws<FormatException>(() => model.GetDouble());
        }

        [Fact]
        public void PowerAppControlPropertyModelForDoubleTest()
        {
            var name = "Count";
            var value = "5";
            var model = new PowerAppControlPropertyModel(name, value);
            Assert.Equal(name, model.Name);
            Assert.Equal(value, model.Value);
            Assert.Equal(FormulaType.String, model.Type);
            Assert.Throws<NotImplementedException>(() => model.GetArrayLength());
            Assert.Equal(value, model.GetString());
            Assert.Throws<NotImplementedException>(() => model[4]);
            Assert.Throws<NotImplementedException>(() => model.TryGetProperty("property", out var result));
            Assert.Throws<FormatException>(() => model.GetBoolean());
            Assert.Equal(5, model.GetDouble());
        }

        [Fact]
        public void PowerAppControlPropertyModelForBooleanTest()
        {
            var name = "IsSelected";
            var value = "true";
            var model = new PowerAppControlPropertyModel(name, value);
            Assert.Equal(name, model.Name);
            Assert.Equal(value, model.Value);
            Assert.Equal(FormulaType.String, model.Type);
            Assert.Throws<NotImplementedException>(() => model.GetArrayLength());
            Assert.Equal(value, model.GetString());
            Assert.Throws<NotImplementedException>(() => model[4]);
            Assert.Throws<NotImplementedException>(() => model.TryGetProperty("property", out var result));
            Assert.True(model.GetBoolean());
            Assert.Throws<FormatException>(() => model.GetDouble());
        }
    }
}
