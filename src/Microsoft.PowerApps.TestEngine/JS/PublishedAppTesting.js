﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

function parseControl(controlName, controlObject) {
    var properties = Object.keys(controlObject.modelProperties);
    var controls = [];
    if (controlObject.controlWidget.replicatedContextManager) {
        var childrenControlContext = controlObject.controlWidget.replicatedContextManager.authoringAreaBindingContext.controlContexts;
        var childControlNames = Object.keys(childrenControlContext);
        childControlNames.forEach((childControlName) => {
            var childControlObject = childrenControlContext[childControlName];
            var childControlsList = parseControl(childControlName, childControlObject);
            controls = controls.concat(childControlsList);
        });
    }

    var componentBindingContext = AppMagic.Controls.GlobalContextManager.bindingContext.componentBindingContexts.lookup(controlName);
    if (componentBindingContext) {
        var componentChildrenControlContext = componentBindingContext.controlContexts;
        var componentChildrenControlNames = Object.keys(componentChildrenControlContext);
        componentChildrenControlNames.forEach((childControlName) => {
            if (childControlName !== controlName) {
                var componentChildControlObject = componentChildrenControlContext[childControlName];
                var childControlsList = parseControl(childControlName, componentChildControlObject);
                controls = controls.concat(childControlsList);
            }
        });
    }

    var propertiesList = [];
    properties.forEach((propertyName) => {
        var propertyType = controlObject.controlWidget.controlProperties[propertyName].propertyType;

        propertiesList.push({ propertyName: propertyName, propertyType: propertyType });
    })

    var control = { name: controlName, properties: propertiesList };
    controls.push(control);

    return controls;
}

function buildControlObjectModel() {

    var controls = [];
    var controlContext = AppMagic.Controls.GlobalContextManager.bindingContext.controlContexts;
    var controlNames = Object.keys(controlContext);
    controlNames.forEach((controlName) => {
        var control = controlContext[controlName];
        var controlList = parseControl(controlName, control);
        controls = controls.concat(controlList);
    });

    return controls;
}

function fetchArrayItemCount(itemPath) {
    if (itemPath.parentControl && itemPath.parentControl.index === null) {
        // Components do not have an item count
        throw "Not a gallery, no item count available. Most likely a component";
    }

    var replicatedContexts = AppMagic.Controls.GlobalContextManager.bindingContext.replicatedContexts

    if (itemPath.parentControl && itemPath.parentControl.index !== null) {
        // Nested gallery - Power Apps only supports one level of nesting so we don't have to go recursively to find it
        // Get parent replicated context
        replicatedContexts = OpenAjax.widget.byId(itemPath.parentControl.controlName).OpenAjax.getAuthoringControlContext()._replicatedContext.bindingContextAt(itemPath.parentControl.index).replicatedContexts;
    }

    var replicatedContext = OpenAjax.widget.byId(itemPath.controlName).OpenAjax.getAuthoringControlContext()._replicatedContext;

    if (!replicatedContext) {
        // This is not a gallery
        throw "Not a gallery, no item count available. Most likely a control";
    }

    var managerId = replicatedContext.manager.managerId;
    return replicatedContexts[managerId].getBindingContextCount()
}

function getBindingContext(itemPath) {
    var bindingContext = AppMagic.Controls.GlobalContextManager.bindingContext;

    if (itemPath.parentControl) {
        // Control is inside a component or gallery
        bindingContext = getBindingContext(itemPath.parentControl);
    }

    if (itemPath.index !== null) {
        // Gallery control
        var managerId = OpenAjax.widget.byId(itemPath.controlName).OpenAjax.getAuthoringControlContext()._replicatedContext.manager.managerId;
        return bindingContext.replicatedContexts[managerId].bindingContextAt(itemPath.index);
    }

    var componentBindingContext = AppMagic.Controls.GlobalContextManager.bindingContext.componentBindingContexts.lookup(itemPath.controlName);

    if (typeof componentBindingContext !== 'undefined') {
        // Component control
        return componentBindingContext;
    }

    return bindingContext;
}

function getPropertyValueFromControl(itemPath) {
    var bindingContext = getBindingContext(itemPath);

    var propertyValue = bindingContext.controlContexts[itemPath.controlName].modelProperties[itemPath.propertyName].getValue();


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

function selectControl(itemPath) {
    var screenId = AppMagic.AuthoringTool.Runtime.getNamedControl(AppMagic.AuthoringTool.Runtime.getCurrentScreenName()).OpenAjax.uniqueId;

    if (itemPath.parentControl && itemPath.parentControl.index !== null ) {
            // Gallery
        var bindingContext = AppMagic.Controls.GlobalContextManager.bindingContext;
        if (itemPath.parentControl.parentControl) {
            // Nested gallery
            bindingContext = getBindingContext(itemPath.parentControl);
            var currentControl = bindingContext.controlContexts[itemPath.controlName].controlWidget;
            return AppMagic.Functions.select(null,
                                    bindingContext,
                                    currentControl.control,
                                    null, // row number
                                    null, // child control
                                    screenId)
        }
        else {
            // One level gallery - the nested gallery approach doesn't work for one level so we have to do it differently
            // select function is starts with 1, while the C# code indexes from 0
            rowOrColumnNumber = itemPath.parentControl.index + 1;
            return AppMagic.Functions.select(null,
                bindingContext,
                AppMagic.AuthoringTool.Runtime.getNamedControl(itemPath.parentControl.controlName),
                rowOrColumnNumber,
                AppMagic.AuthoringTool.Runtime.getNamedControl(itemPath.controlName).OpenAjax._icontrol,
                screenId)
        }        
    }
        /*
        if (parentControlName) {
            var bindingContext = AppMagic.Controls.GlobalContextManager.bindingContext.componentBindingContexts.lookup(parentControlName);
            var buttonWidget = bindingContext.controlContexts[controlName].controlWidget;
            var controlContext = buttonWidget.getOnSelectControlContext(bindingContext);
            buttonWidget.select(controlContext);
            return true;
        } */
    return AppMagic.Functions.select(null,
        AppMagic.Controls.GlobalContextManager.bindingContext,
        AppMagic.AuthoringTool.Runtime.getNamedControl(itemPath.controlName),
        null,
        null,
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