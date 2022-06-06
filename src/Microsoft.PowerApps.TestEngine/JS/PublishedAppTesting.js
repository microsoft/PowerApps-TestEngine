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

function fetchArrayItemCount(itemPath, replicatedContexts) {
    if (!replicatedContexts) {
        // Use global one if not specified
        replicatedContexts = AppMagic.Controls.GlobalContextManager.bindingContext.replicatedContexts
    }

    if (itemPath.childControl && itemPath.index != null) {
        // Nested gallery
        // Get parent replicated context
        replicatedContexts = OpenAjax.widget.byId(itemPath.controlName).OpenAjax.getAuthoringControlContext()._replicatedContext.bindingContextAt(itemPath.index).replicatedContexts;
        return fetchArrayItemCount(itemPath.childControl, replicatedContexts);
    }

    if (itemPath.childControl) {
        // Components do not have an item count
        throw "Not a gallery, no item count available. Most likely a component";
    }

    var replicatedContext = OpenAjax.widget.byId(itemPath.controlName).OpenAjax.getAuthoringControlContext()._replicatedContext;

    if (!replicatedContext) {
        // This is not a gallery
        throw "Not a gallery, no item count available. Most likely a control";
    }

    // Bottom level gallery
    var managerId = replicatedContext.manager.managerId;
    return replicatedContexts[managerId].getBindingContextCount();
}

function getControlAndContext(itemPath, contextToUse, parentIndex, parentControl) {
    if (contextToUse === null) {
        contextToUse = AppMagic.Controls.GlobalContextManager.bindingContext;
    }

    if (itemPath.index !== null && itemPath.childControl) {
        // Gallery
        var galleryControl = AppMagic.AuthoringTool.Runtime.getNamedControl(itemPath.controlName, contextToUse);
        var replicatedContextManagerId = galleryControl.OpenAjax.replicatedContextManager.managerId;
        var galleryBindingContext = galleryControl.OpenAjax.replicatedContextManager.authoringAreaBindingContext.parent.replicatedContexts[replicatedContextManagerId].bindingContextAt(itemPath.index);
        parentIndex = itemPath.index;
        parentControl = galleryControl;
        return getControlAndContext(itemPath.childControl, galleryBindingContext, parentIndex, parentControl);
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
            propertyName: itemPath.propertyName,
            controlName: itemPath.controlName,
            parentIndex: parentIndex,
            parentControl: parentControl
        }
    }
}

function getPropertyValueFromControl(itemPath, bindingContext) {
    var propertyValue = null;

    if (!bindingContext) {
        // Use global one if not specified
        bindingContext = AppMagic.Controls.GlobalContextManager.bindingContext
    }

    if (itemPath.childControl && itemPath.index != null) {
        // Gallery
        var managerId = OpenAjax.widget.byId(itemPath.controlName).OpenAjax.getAuthoringControlContext()._replicatedContext.manager.managerId;
        bindingContext = bindingContext.replicatedContexts[managerId].bindingContextAt(itemPath.index);
        return getPropertyValueFromControl(itemPath.childControl, bindingContext);
    }

    if (itemPath.childControl) {
        // Components

        // TODO: handle components
        /*
        if (parentControlName) {
            propertyValue = (AppMagic.AuthoringTool.Runtime.getNamedControl(controlName).OpenAjax.getPropertyValue(propertyName, AppMagic.Controls.GlobalContextManager.bindingContext.componentBindingContexts.lookup(parentControlName)));
        }*/
        throw "Not implemented";
    }

    propertyValue = bindingContext.controlContexts[itemPath.controlName].modelProperties[itemPath.propertyName].getValue();


    return JSON.stringify({
        propertyValue: propertyValue
    });
}

function selectControl(itemPath, bindingContext) {

    if (!bindingContext) {
        // Use global one if not specified
        bindingContext = AppMagic.Controls.GlobalContextManager.bindingContext
    }


    if (itemPath.childControl && itemPath.index != null) {
        // Gallery
        var managerId = OpenAjax.widget.byId(itemPath.controlName).OpenAjax.getAuthoringControlContext()._replicatedContext.manager.managerId;
        bindingContext = bindingContext.replicatedContexts[managerId].bindingContextAt(itemPath.index);
        return selectControl(itemPath.childControl, bindingContext);
    }

    if (itemPath.childControl) {
        // Components - TODO

    //    var bindingContext = AppMagic.Controls.GlobalContextManager.bindingContext.componentBindingContexts.lookup(parentControlName);
    //    var buttonWidget = bindingContext.controlContexts[controlName].controlWidget;
    //    var controlContext = buttonWidget.getOnSelectControlContext(bindingContext);
    //    buttonWidget.select(controlContext);
    //    return true;
        throw "Not implemented";
    }


    var screenId = AppMagic.AuthoringTool.Runtime.getNamedControl(AppMagic.AuthoringTool.Runtime.getCurrentScreenName()).OpenAjax.uniqueId;
    var currentControl = bindingContext.controlContexts[itemPath.controlName].controlWidget;

    return AppMagic.Functions.select(null,
                                    bindingContext,  
                                    currentControl.control, 
                                    null, // row number
                                    null, // child control
                                    screenId);
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