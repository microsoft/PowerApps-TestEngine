// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestEngine.PowerApps;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerApps
{
    public class PowerAppControlModelTests
    {

        [Fact]
        public void PowerAppControlModelTest()
        {
            var mockPowerAppFunctions = new Mock<IPowerAppFunctions>(MockBehavior.Strict);
            var jsProperty = new JSPropertyValueModel() { PropertyType = "string", PropertyValue = "Hello" };
            mockPowerAppFunctions.Setup(x => x.GetPropertyValueFromControlAsync<string>(It.IsAny<ItemPath>())).Returns(Task.FromResult(JsonConvert.SerializeObject(jsProperty)));
            var name = "Label";
            var properties = new List<string>() { "Text", "X", "Y" };
            var model = new PowerAppControlModel(name, properties, mockPowerAppFunctions.Object);
            Assert.Equal(name, model.Name);
            Assert.Equal(properties, model.Properties);
            Assert.Throws<NotImplementedException>(() => model.GetArrayLength());
            Assert.Throws<NotImplementedException>(() => model[4]);
            Assert.Throws<NotImplementedException>(() => model.GetString());
            Assert.Throws<NotImplementedException>(() => model.GetBoolean());
            Assert.Throws<NotImplementedException>(() => model.GetDouble());
            Assert.True(model.TryGetProperty("Text", out var property));
            Assert.NotNull(property);
            Assert.Equal("Text", (property as PowerAppControlPropertyModel).Name);
            Assert.Equal(jsProperty.PropertyValue, (property as PowerAppControlPropertyModel).Value);
            Assert.True(model.TryGetProperty("X", out property));
            Assert.NotNull(property);
            Assert.Equal("X", (property as PowerAppControlPropertyModel).Name);
            Assert.Equal(jsProperty.PropertyValue, (property as PowerAppControlPropertyModel).Value);
            Assert.True(model.TryGetProperty("Y", out property));
            Assert.NotNull(property);
            Assert.Equal("Y", (property as PowerAppControlPropertyModel).Name);
            Assert.Equal(jsProperty.PropertyValue, (property as PowerAppControlPropertyModel).Value);
            Assert.False(model.TryGetProperty("NonExistentProperty", out property));
            Assert.Null(property);
        }
    }
}
