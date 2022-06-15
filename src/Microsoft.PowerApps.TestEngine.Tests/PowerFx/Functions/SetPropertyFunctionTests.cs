// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.PowerApps.PowerFxModel;
using Microsoft.PowerApps.TestEngine.PowerFx;
using Microsoft.PowerApps.TestEngine.PowerFx.Functions;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Microsoft.PowerFx.Types;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerFx.Functions
{
    public class SetPropertyFunctionTests
    {
        private Mock<IPowerAppFunctions> MockPowerAppFunctions;
        private Mock<ITestState> MockTestState;

        public SetPropertyFunctionTests()
        {
            MockPowerAppFunctions = new Mock<IPowerAppFunctions>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
        }

        [Fact]
        public void SetPropertyFunctionThrowsOnNullObjectTest()
        {
            var recordType = new RecordType().Add("Text", FormulaType.String);
            var SetPropertyFunction = new SetPropertyFunction(MockPowerAppFunctions.Object, recordType);
            Assert.ThrowsAny<Exception>(() => SetPropertyFunction.Execute(null, null, null));
        }

        [Fact]
        public void SetPropertyFunctionThrowsOnNonPowerAppsRecordValuetTest()
        {
            var recordType = new RecordType().Add("Text", FormulaType.String);
            var SetPropertyFunction = new SetPropertyFunction(MockPowerAppFunctions.Object, recordType);

            var someOtherRecordValue = new SomeOtherRecordValue(recordType);
            Assert.ThrowsAny<Exception>(() => SetPropertyFunction.Execute(someOtherRecordValue, (StringValue)"Text", (StringValue)"10"));
        }

        [Fact]
        public void SetPropertyFunctionTest()
        {
            // Assert(TextInput1.Text = "5", "Validate default number of columns to generate is 5");

            // Select(Button1);
            // Assert(Index(Gallery1.AllItems, 1).Label4.Text = "Row id: 1", "Validate row label");
            // Assert(Index(Index(Gallery1.AllItems, 1).Gallery2.AllItems, 5).Label5.Text = "Column id: 5", "Validate the label in the nested gallery");

            // SetProperty(TextInput1, "Text", "10");
            // Select(Button1);
            // Assert(Index(Gallery1.AllItems, 2).Label4.Text = "Row id: 2", "Validate row label");
            // Assert(Index(Index(Gallery1.AllItems, 2).Gallery2.AllItems, 10).Label5.Text = "Column id: 10", "Validate the label in the nested gallery");
        }
    }
}
