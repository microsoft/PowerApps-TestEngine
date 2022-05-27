﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.PowerFx.Functions;
using Microsoft.PowerFx.Core.Public.Values;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerFx.Functions
{
    public class WaitFunctionTests
    {
        private Mock<IPowerAppFunctions> MockPowerAppFunctions;

        public WaitFunctionTests()
        {
            MockPowerAppFunctions = new Mock<IPowerAppFunctions>(MockBehavior.Strict);
        }

        [Fact]
        public void WaitFunctionThrowsOnInvalidArgumentsTest()
        {
            var powerAppsObject = new PowerAppControlModel("Label1", new List<string>() { "Text", "Color" }, MockPowerAppFunctions.Object);
            var waitFunction = new WaitFunction();
            Assert.Throws<ArgumentNullException>(() => waitFunction.Execute(null, FormulaValue.New("Text"), FormulaValue.New("1")));
            Assert.Throws<ArgumentNullException>(() => waitFunction.Execute(FormulaValue.New(new SomeOtherUntypedObject()), null, FormulaValue.New("1")));
            Assert.Throws<ArgumentNullException>(() => waitFunction.Execute(FormulaValue.New(new SomeOtherUntypedObject()), FormulaValue.New("Text"), null));
            Assert.Throws<InvalidCastException>(() => waitFunction.Execute(FormulaValue.New(new SomeOtherUntypedObject()), FormulaValue.New("Text"), FormulaValue.New("1")));
            Assert.Throws<InvalidOperationException>(() => waitFunction.Execute(FormulaValue.New(powerAppsObject), FormulaValue.New("NonExistentProperty"), FormulaValue.New("1")));
        }

        [Fact]
        public void WaitFunctionSucceedsTest()
        {
            var textToWaitFor = "1";
            var powerAppsObject = new PowerAppControlModel("Label1", new List<string>() { "Text", "Color" }, MockPowerAppFunctions.Object);
            var jsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyType = "string",
                PropertyValue = textToWaitFor,
            };
            var expectedItemPath = new ItemPath
            {
                ControlName = "Label1",
                PropertyName = "Text"
            };
            MockPowerAppFunctions.Setup(x => x.GetPropertyValueFromControlAsync<string>(It.IsAny<ItemPath>()))
                    .Returns(Task.FromResult(JsonConvert.SerializeObject(jsPropertyValueModel)));

            var waitFunction = new WaitFunction();
            waitFunction.Execute(FormulaValue.New(powerAppsObject), FormulaValue.New("Text"), FormulaValue.New(textToWaitFor));
            
            MockPowerAppFunctions.Verify(x => x.GetPropertyValueFromControlAsync<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == expectedItemPath.ControlName && itemPath.PropertyName == expectedItemPath.PropertyName)), Times.Once());
        }

        [Fact]
        public void WaitFunctionWaitsForValueToUpdateTest()
        {
            var textToWaitFor = "1";
            var powerAppsObject = new PowerAppControlModel("Label1", new List<string>() { "Text", "Color" }, MockPowerAppFunctions.Object);
            var jsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyType = "string",
                PropertyValue = "0",
            };
            var finalJsPropertyValueModel = new JSPropertyValueModel()
            {
                PropertyType = "string",
                PropertyValue = textToWaitFor,
            };
            var expectedItemPath = new ItemPath
            {
                ControlName = "Label1",
                PropertyName = "Text"
            };
            MockPowerAppFunctions.SetupSequence(x => x.GetPropertyValueFromControlAsync<string>(It.IsAny<ItemPath>()))
                    .Returns(Task.FromResult(JsonConvert.SerializeObject(jsPropertyValueModel)))
                    .Returns(Task.FromResult(JsonConvert.SerializeObject(jsPropertyValueModel)))
                    .Returns(Task.FromResult(JsonConvert.SerializeObject(finalJsPropertyValueModel)));

            var waitFunction = new WaitFunction();
            waitFunction.Execute(FormulaValue.New(powerAppsObject), FormulaValue.New("Text"), FormulaValue.New(textToWaitFor));

            MockPowerAppFunctions.Verify(x => x.GetPropertyValueFromControlAsync<string>(It.Is<ItemPath>((itemPath) => itemPath.ControlName == expectedItemPath.ControlName && itemPath.PropertyName == expectedItemPath.PropertyName)), Times.Exactly(3));
        }
    }
}
