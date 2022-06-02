// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

function parseControl(controlName, controlObject) {
    var properties = Object.keys(controlObject.modelProperties);
    var childControls = [];
    var itemCount = null;
    var isArray = false;
    if (controlObject.controlWidget.replicatedContextManager) {
        var childrenControlContext = controlObject.controlWidget.replicatedContextManager.authoringAreaBindingContext.controlContexts;
        var childControlNames = Object.keys(childrenControlContext);
        childControlNames.forEach((childControlName) => {
            var childControlObject = childrenControlContext[childControlName];
            var childControlModel = parseControl(childControlName, childControlObject);
            childControls.push(childControlModel);
        });

        var managerId = controlObject.controlWidget.replicatedContextManager.managerId;
        var replicatedContext = AppMagic.Controls.GlobalContextManager.bindingContext.replicatedContexts[managerId];
        itemCount = replicatedContext.getBindingContextCount();
        isArray = true;
    }

    var componentBindingContext = AppMagic.Controls.GlobalContextManager.bindingContext.componentBindingContexts.lookup(controlName);
    if (componentBindingContext) {
        var componentChildrenControlContext = componentBindingContext.controlContexts;
        var componentChildrenControlNames = Object.keys(componentChildrenControlContext);
        componentChildrenControlNames.forEach((childControlName) => {
            if (childControlName !== controlName) {
                var componentChildControlObject = componentChildrenControlContext[childControlName];
                var childControlModel = parseControl(childControlName, componentChildControlObject);
                childControls.push(childControlModel);
            }
        });
    }

    var propertiesList = [];
    properties.forEach((propertyName) => {
        var shortPropertyType = controlObject.controlWidget.controlProperties[propertyName].propertyType;
        // The property type should match types listed here: https://github.com/microsoft/Power-Fx/blob/main/src/libraries/Microsoft.PowerFx.Core/Public/Types/FormulaType.cs
        var propertyType = "Unknown"; // Default to unknown if we don't know how to map it

        switch (shortPropertyType) {
            case (AppMagic.Schema.TypeString):
                propertyType = "String";
                break;
            case (AppMagic.Schema.TypeBoolean):
                propertyType = "Boolean";
                break;
            case (AppMagic.Schema.TypeNumber):
                propertyType = "Number";
                break;
            case (AppMagic.Schema.TypeTime):
                propertyType = "Time";
                break;
            case (AppMagic.Schema.TypeDate):
                propertyType = "Date";
                break;
            case (AppMagic.Schema.TypeDateTime):
                propertyType = "DateTime";
                break;
            case (AppMagic.Schema.TypeDateTimeNoTimeZone):
                propertyType = "DateTimeNoTimeZone";
                break;
            case (AppMagic.Schema.TypeHyperlink):
                propertyType = "Hyperlink";
                break;
            case (AppMagic.Schema.TypeGuid):
                propertyType = "Guid";
                break;
        }

        propertiesList.push({ propertyName: propertyName, propertyType: propertyType });
    })

    var controlModel = { name: controlName, properties: propertiesList, childrenControls: childControls, itemCount: itemCount, isArray: isArray };

    return controlModel;
}

function buildControlObjectModel() {

    var controls = [];
    var controlContext = AppMagic.Controls.GlobalContextManager.bindingContext.controlContexts;
    var controlNames = Object.keys(controlContext);
    controlNames.forEach((controlName) => {
        var control = controlContext[controlName];
        var controlModel = parseControl(controlName, control);
        controls.push(controlModel);
    });
    return controls;
}

function getControlAndContext(itemPath, contextToUse) {
    if (contextToUse === null) {
        contextToUse = AppMagic.Controls.GlobalContextManager.bindingContext;
    }

    if (itemPath.index && itemPath.childControl) {
        // Gallery
        var galleryControl = AppMagic.AuthoringTool.Runtime.getNamedControl(itemPath.controlName, contextToUse);
        var replicatedContextManagerId = galleryControl.OpenAjax.replicatedContextManager.managerId;
        var galleryBindingContext = galleryControl.OpenAjax.replicatedContextManager.authoringAreaBindingContext.parent.replicatedContexts[replicatedContextManagerId].bindingContextAt(itemPath.index);
        return getControlAndContext(itemPath.childControl, galleryBindingContext);
    }
    else if (itemPath.childControl) {
        // Component
    }
    else {
        // Just a regular control
        var control = AppMagic.AuthoringTool.Runtime.getNamedControl(itemPath.controlName, contextToUse);
        return {
            control: control,
            context: contextToUse,
            propertyName: itemPath.propertyName
        }
    }
}

function getPropertyValueFromControl(itemPath) {
    var propertyValue = null;

    // TODO: handle galleries and components
    /*
    if (parentControlName && rowOrColumnNumber) {
        propertyValue = (AppMagic.AuthoringTool.Runtime.getNamedControl(parentControlName, AppMagic.Controls.GlobalContextManager.bindingContext).OpenAjax.getPropertyValue("AllItems"))[rowOrColumnNumber][controlName][propertyName];
    }

    if (parentControlName) {
        propertyValue = (AppMagic.AuthoringTool.Runtime.getNamedControl(controlName).OpenAjax.getPropertyValue(propertyName, AppMagic.Controls.GlobalContextManager.bindingContext.componentBindingContexts.lookup(parentControlName)));
    }*/

    var controlAndContext = getControlAndContext(itemPath, null);
    propertyValue = controlAndContext.control.OpenAjax.getPropertyValue(controlAndContext.propertyName, controlAndContext.context);

    if (!propertyValue) {
        return JSON.stringify({ propertyValue: null });
    }

    return JSON.stringify({
        propertyValue: propertyValue
    });
}

function selectControl(itemPath) {
    // TODO: handle nested galleries and components
    // Can we use the getControlAndContext method?

    if (itemPath.index && itemPath.childControl) {
        // Gallery
        // select function is starts with 1, while the C# code indexes from 0
        rowOrColumnNumber = itemPath.index + 1;
        return AppMagic.Functions.select(null, AppMagic.Controls.GlobalContextManager.bindingContext, AppMagic.AuthoringTool.Runtime.getNamedControl(itemPath.controlName), rowOrColumnNumber, AppMagic.AuthoringTool.Runtime.getNamedControl(itemPath.childControl.controlName).OpenAjax._icontrol, AppMagic.AuthoringTool.Runtime.getNamedControl(AppMagic.AuthoringTool.Runtime.getCurrentScreenName()).OpenAjax.uniqueId)
    }
    /*
    if (parentControlName) {
        var bindingContext = AppMagic.Controls.GlobalContextManager.bindingContext.componentBindingContexts.lookup(parentControlName);
        var buttonWidget = bindingContext.controlContexts[controlName].controlWidget;
        var controlContext = buttonWidget.getOnSelectControlContext(bindingContext);
        buttonWidget.select(controlContext);
        return true;
    } */
    return AppMagic.Functions.select(null, AppMagic.Controls.GlobalContextManager.bindingContext, AppMagic.AuthoringTool.Runtime.getNamedControl(itemPath.controlName), null, null, AppMagic.AuthoringTool.Runtime.getNamedControl(AppMagic.AuthoringTool.Runtime.getCurrentScreenName()).OpenAjax.uniqueId);
}

function setPropertyValueForControl(itemPath, value) {
    // TODO: handle galleries and components
    /*
    if (parentControlName && rowOrColumnNumber) {
        var galleryControlOpenAjax = AppMagic.AuthoringTool.Runtime.getNamedControl(parentControlName).OpenAjax;
        var replicatedContextManagerId = galleryControlOpenAjax.replicatedContextManager.managerId;
        var galleryBindingContext = galleryControlOpenAjax.replicatedContextManager.authoringAreaBindingContext.parent.replicatedContexts[replicatedContextManagerId].bindingContextAt(rowOrColumnNumber);
        return AppMagic.AuthoringTool.Runtime.getNamedControl(controlName).OpenAjax.setPropertyValueInternal(propertyName, value, galleryBindingContext)
    }

    if (parentControlName) {

        return (AppMagic.AuthoringTool.Runtime.getNamedControl(controlName).OpenAjax.setPropertyValueInternal(propertyName, value, AppMagic.Controls.GlobalContextManager.bindingContext.componentBindingContexts.lookup(parentControlName)));
    }*/
    return AppMagic.AuthoringTool.Runtime.getNamedControl(itemPath.controlName, AppMagic.Controls.GlobalContextManager.bindingContext).OpenAjax.setPropertyValueInternal(itemPath.propertyName, value, AppMagic.Controls.GlobalContextManager.bindingContext);
}