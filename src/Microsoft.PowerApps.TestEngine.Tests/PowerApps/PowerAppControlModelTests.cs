// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Microsoft.PowerFx.Core.Public.Types;
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
        private void AssertTryGetProperty(PowerAppControlModel model, string propertyToFetch, string? expectedPropertyResult, bool expectedTryGetResult, FormulaType? expectedPropertyType)
        {
            Assert.Equal(expectedTryGetResult, model.TryGetProperty(propertyToFetch, out var property));
            if (expectedPropertyResult != null)
            {
                Assert.NotNull(property);
                Assert.Equal(propertyToFetch, (property as PowerAppControlPropertyModel).Name);
                Assert.Equal(expectedPropertyResult, (property as PowerAppControlPropertyModel).Value);
                Assert.Equal(expectedPropertyType, (property as PowerAppControlPropertyModel).Type);
            }
            else
            {
                Assert.Null(property);
            }
        }

        [Fact]
        public void PowerAppControlModelTest()
        {
            var mockPowerAppFunctions = new Mock<IPowerAppFunctions>(MockBehavior.Strict);
            var jsProperty = new JSPropertyValueModel() { PropertyValue = "Hello" };
            mockPowerAppFunctions.Setup(x => x.GetPropertyValueFromControlAsync<string>(It.IsAny<ItemPath>())).Returns(Task.FromResult(JsonConvert.SerializeObject(jsProperty)));
            var name = "Label";
            var properties = TestData.CreateRandomPropertiesDictionary();
            var model = new PowerAppControlModel(name, properties, mockPowerAppFunctions.Object);
            Assert.Equal(name, model.Name);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(ExternalTypeKind.Object, (model.Type as ExternalType).Kind);
            Assert.False(model.IsArray);
            Assert.False(model.SelectedIndex.HasValue);
            Assert.Empty(model.ChildControls);
            Assert.Null(model.ParentControl);
            Assert.Throws<NotImplementedException>(() => model.GetArrayLength());
            Assert.Throws<NotImplementedException>(() => model[4]);
            Assert.Throws<NotImplementedException>(() => model.GetString());
            Assert.Throws<NotImplementedException>(() => model.GetBoolean());
            Assert.Throws<NotImplementedException>(() => model.GetDouble());

            foreach(var property in properties)
            {
                AssertTryGetProperty(model, property.Key, jsProperty.PropertyValue, true, property.Value);
            }
            AssertTryGetProperty(model, "NonExistentProperty", null, false, null);

            var itemPath = model.CreateItemPath();
            Assert.NotNull(itemPath);
            Assert.Equal(name, itemPath.ControlName);
            Assert.False(itemPath.Index.HasValue);
            Assert.Null(itemPath.ChildControl);
            Assert.Null(itemPath.PropertyName);

            var propertyName = "Text";
            var itemPathWithPropertyName = model.CreateItemPath(propertyName: propertyName);
            Assert.NotNull(itemPathWithPropertyName);
            Assert.Equal(name, itemPathWithPropertyName.ControlName);
            Assert.False(itemPathWithPropertyName.Index.HasValue);
            Assert.Null(itemPathWithPropertyName.ChildControl);
            Assert.Equal(propertyName, itemPathWithPropertyName.PropertyName);
        }


        [Fact]
        public void PowerAppControlModelWithArrayTest()
        {
            var propertyValues = new Dictionary<string, string>();
            var childProperties = TestData.CreateRandomPropertiesDictionary();
            var arrayProperties = TestData.CreateRandomPropertiesDictionary();

            foreach (var property in childProperties)
            {
                propertyValues.Add(property.Key, Guid.NewGuid().ToString());
            }

            foreach (var property in arrayProperties)
            {
                propertyValues.Add(property.Key, Guid.NewGuid().ToString());
            }

            var mockPowerAppFunctions = new Mock<IPowerAppFunctions>(MockBehavior.Strict);

            foreach(var propertyValue in propertyValues)
            {
                var jsProperty = new JSPropertyValueModel() { PropertyValue = propertyValue.Value };
                mockPowerAppFunctions.Setup(
                    x => x.GetPropertyValueFromControlAsync<string>(It.Is<ItemPath>((x) => x.PropertyName == propertyValue.Key || (x.ChildControl != null && x.ChildControl.PropertyName == propertyValue.Key))))
                    .Returns(Task.FromResult(JsonConvert.SerializeObject(jsProperty)));
            }

            var arrayItemCount = 5;
            mockPowerAppFunctions.Setup(x => x.GetItemCountAsync(It.IsAny<ItemPath>())).Returns(Task.FromResult(arrayItemCount));


            var childName = "Label";
            var childModel = new PowerAppControlModel(childName, childProperties, mockPowerAppFunctions.Object);
            Assert.Equal(childName, childModel.Name);
            Assert.Equal(childProperties, childModel.Properties);
            Assert.Equal(ExternalTypeKind.Object, (childModel.Type as ExternalType).Kind);
            Assert.False(childModel.IsArray);
            Assert.False(childModel.SelectedIndex.HasValue);
            Assert.Empty(childModel.ChildControls);
            Assert.Null(childModel.ParentControl);
            Assert.Throws<NotImplementedException>(() => childModel.GetArrayLength());
            Assert.Throws<NotImplementedException>(() => childModel[4]);
            Assert.Throws<NotImplementedException>(() => childModel.GetString());
            Assert.Throws<NotImplementedException>(() => childModel.GetBoolean());
            Assert.Throws<NotImplementedException>(() => childModel.GetDouble());

            foreach (var property in childProperties)
            {
                AssertTryGetProperty(childModel, property.Key, propertyValues[property.Key], true, property.Value);
            }
            AssertTryGetProperty(childModel, "NonExistentProperty", null, false, null);

            var arrayName = "Gallery";
            var arrayModel = new PowerAppControlModel(arrayName, arrayProperties, mockPowerAppFunctions.Object);
            arrayModel.IsArray = true;
            arrayModel.AddChildControl(childModel);
            Assert.Equal(arrayName, arrayModel.Name);
            Assert.Equal(arrayProperties, arrayModel.Properties);
            Assert.Equal(ExternalTypeKind.Array, (arrayModel.Type as ExternalType).Kind);
            Assert.False(arrayModel.SelectedIndex.HasValue);
            Assert.Single(arrayModel.ChildControls);
            Assert.Equal(arrayItemCount, arrayModel.GetArrayLength());
            Assert.Equal(childName, arrayModel.ChildControls[0].Name);
            Assert.Equal(arrayName, arrayModel.ChildControls[0].ParentControl.Name);
            Assert.Null(arrayModel.ParentControl);
            Assert.Equal(arrayItemCount, arrayModel.GetArrayLength());
            Assert.Throws<NotImplementedException>(() => arrayModel.GetString());
            Assert.Throws<NotImplementedException>(() => arrayModel.GetBoolean());
            Assert.Throws<NotImplementedException>(() => arrayModel.GetDouble());

            foreach (var property in arrayProperties)
            {
                AssertTryGetProperty(arrayModel, property.Key, propertyValues[property.Key], true, property.Value);
            }

            // Unable to fetch non existent properties
            AssertTryGetProperty(arrayModel, "NonExistentProperty", null, false, null);

            // Unable to fetch child properties from the array model as you have to index into the child
            foreach (var property in childProperties)
            {
                AssertTryGetProperty(arrayModel, property.Key, null, false, null);
            }

            // Unable to fetch the child because we haven't indexed into the array model yet
            AssertTryGetProperty(arrayModel, childModel.Name, null, false, null);

            // Item path tests

            for (var i = 0; i < arrayItemCount; i++)
            {
                var indexedArrayModel = arrayModel[i] as PowerAppControlModel;
                Assert.NotNull(indexedArrayModel);
                Assert.Equal(arrayName, indexedArrayModel.Name);
                Assert.Equal(arrayProperties, indexedArrayModel.Properties);
                Assert.Equal(ExternalTypeKind.Object, (indexedArrayModel.Type as ExternalType).Kind);
                Assert.True(indexedArrayModel.IsArray);
                Assert.Equal(i, indexedArrayModel.SelectedIndex);
                Assert.Single(indexedArrayModel.ChildControls);
                Assert.Equal(childName, indexedArrayModel.ChildControls[0].Name);
                Assert.Equal(arrayName, indexedArrayModel.ChildControls[0].ParentControl.Name);
                Assert.Equal(i, indexedArrayModel.ChildControls[0].ParentControl.SelectedIndex);
                Assert.Null(indexedArrayModel.ParentControl);
                Assert.Equal(arrayItemCount, indexedArrayModel.GetArrayLength());
                Assert.Throws<NotImplementedException>(() => indexedArrayModel[arrayItemCount]);
                Assert.Throws<NotImplementedException>(() => indexedArrayModel[arrayItemCount + 5]);
                Assert.Throws<NotImplementedException>(() => indexedArrayModel.GetString());
                Assert.Throws<NotImplementedException>(() => indexedArrayModel.GetBoolean());
                Assert.Throws<NotImplementedException>(() => indexedArrayModel.GetDouble());

                foreach (var property in arrayProperties)
                {
                    AssertTryGetProperty(indexedArrayModel, property.Key, propertyValues[property.Key], true, property.Value);
                }

                // Unable to fetch non existent properties
                AssertTryGetProperty(indexedArrayModel, "NonExistentProperty", null, false, null);

                // Unable to fetch child properties from the array model as you have to index into the child
                foreach (var property in childProperties)
                {
                    AssertTryGetProperty(indexedArrayModel, property.Key, null, false, null);
                }

                var itemPath = indexedArrayModel.CreateItemPath();
                Assert.NotNull(itemPath);
                Assert.Equal(arrayName, itemPath.ControlName);
                Assert.Equal(i, itemPath.Index);
                Assert.Null(itemPath.ChildControl);
                Assert.Null(itemPath.PropertyName);

                var propertyName = "Text";
                var itemPathWithPropertyName = indexedArrayModel.CreateItemPath(propertyName: propertyName);
                Assert.NotNull(itemPathWithPropertyName);
                Assert.Equal(arrayName, itemPathWithPropertyName.ControlName);
                Assert.Equal(i, itemPathWithPropertyName.Index);
                Assert.Null(itemPathWithPropertyName.ChildControl);
                Assert.Equal(propertyName, itemPathWithPropertyName.PropertyName);

                // fetch child model
                Assert.True(indexedArrayModel.TryGetProperty(childModel.Name, out var outModel));
                var indexedChildModel = outModel as PowerAppControlModel;
                Assert.NotNull(indexedChildModel);
                Assert.Equal(childName, indexedChildModel.Name);
                Assert.False(indexedChildModel.SelectedIndex.HasValue);

                var childItemPath = indexedChildModel.CreateItemPath();
                Assert.NotNull(childItemPath);
                Assert.Equal(arrayName, childItemPath.ControlName);
                Assert.Equal(i, childItemPath.Index);
                Assert.NotNull(childItemPath.ChildControl);
                Assert.Equal(childName, childItemPath.ChildControl.ControlName);
                Assert.Null(childItemPath.ChildControl.PropertyName);
                Assert.Null(childItemPath.ChildControl.ChildControl);
                Assert.False(childItemPath.ChildControl.Index.HasValue);
                Assert.Null(childItemPath.PropertyName);

                foreach (var property in childProperties)
                {
                    AssertTryGetProperty(indexedChildModel, property.Key, propertyValues[property.Key], true, property.Value);

                    var childItemPathWithPropertyName = indexedChildModel.CreateItemPath(propertyName: property.Key);
                    Assert.NotNull(childItemPathWithPropertyName);
                    Assert.Equal(arrayName, childItemPathWithPropertyName.ControlName);
                    Assert.Equal(i, childItemPathWithPropertyName.Index);
                    Assert.NotNull(childItemPathWithPropertyName.ChildControl);
                    Assert.Equal(childName, childItemPathWithPropertyName.ChildControl.ControlName);
                    Assert.Equal(childItemPathWithPropertyName.ChildControl.PropertyName, property.Key);
                    Assert.Null(childItemPathWithPropertyName.ChildControl.ChildControl);
                    Assert.False(childItemPathWithPropertyName.ChildControl.Index.HasValue);
                    Assert.Null(childItemPathWithPropertyName.PropertyName);
                }
            }
        }
    }
}
