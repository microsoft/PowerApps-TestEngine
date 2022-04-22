// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

function getContainer() {

    if (window['AppDependencyHandler']) {
        return AppDependencyHandler.containers[Object.keys(AppDependencyHandler.containers)[0]];
    }

    return window;
}

function parseControl(controlName, controlObject) {
    var properties = Object.keys(controlObject.modelProperties);
    var childControls = [];
    var itemCount = 0;
    if (controlObject.controlWidget.replicatedContextManager) {
        var childrenControlContext = controlObject.controlWidget.replicatedContextManager.authoringAreaBindingContext.controlContexts;
        var childControlNames = Object.keys(childrenControlContext);
        childControlNames.forEach((childControlName) => {
            var childControlObject = childrenControlContext[childControlName];
            var childControlModel = parseControl(childControlName, childControlObject);
            childControls.push(childControlModel);
        });

        var managerId = controlObject.controlWidget.replicatedContextManager.managerId;
        var replicatedContext = getContainer().AppMagic.Controls.GlobalContextManager.bindingContext.replicatedContexts[managerId];
        itemCount = replicatedContext.getBindingContextCount();
    }

    var componentBindingContext = getContainer().AppMagic.Controls.GlobalContextManager.bindingContext.componentBindingContexts.lookup(controlName);
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

    var controlModel = { name: controlName, properties: properties, childrenControls: childControls, itemCount: itemCount };

    return controlModel;
}

function buildControlObjectModel() {

    var controls = [];
    var controlContext = getContainer().AppMagic.Controls.GlobalContextManager.bindingContext.controlContexts;
    var controlNames = Object.keys(controlContext);
    controlNames.forEach((controlName) => {
        var control = controlContext[controlName];
        var controlModel = parseControl(controlName, control);
        controls.push(controlModel);
    });
    return controls;
}

function getPropertyValueFromControl(controlName, propertyName, parentControlName, rowOrColumnNumber) {
    var propertyValue = null;
    if (parentControlName && rowOrColumnNumber) {
        propertyValue = (getContainer().AppMagic.AuthoringTool.Runtime.getNamedControl(parentControlName, getContainer().AppMagic.Controls.GlobalContextManager.bindingContext).OpenAjax.getPropertyValue("AllItems"))[rowOrColumnNumber][controlName][propertyName];
    }

    if (parentControlName) {
        propertyValue = (getContainer().AppMagic.AuthoringTool.Runtime.getNamedControl(controlName).OpenAjax.getPropertyValue(propertyName, getContainer().AppMagic.Controls.GlobalContextManager.bindingContext.componentBindingContexts.lookup(parentControlName)));
    }
    propertyValue = (getContainer().AppMagic.AuthoringTool.Runtime.getNamedControl(controlName, getContainer().AppMagic.Controls.GlobalContextManager.bindingContext).OpenAjax.getPropertyValue(propertyName));

    if (!propertyValue) {
        return JSON.stringify({ propertyValue: null, propertyType: null });
    }

    return JSON.stringify({
        propertyValue: propertyValue, propertyType: typeof propertyValue
    });
}

function selectControl(controlName, parentControlName, rowOrColumnNumber) {
    if (parentControlName && rowOrColumnNumber) {
        // select function is starts with 1, while the C# code indexes from 0
        rowOrColumnNumber++;
        return getContainer().AppMagic.Functions.select(null, getContainer().AppMagic.Controls.GlobalContextManager.bindingContext, getContainer().AppMagic.AuthoringTool.Runtime.getNamedControl(parentControlName), rowOrColumnNumber, getContainer().AppMagic.AuthoringTool.Runtime.getNamedControl(controlName).OpenAjax._icontrol, getContainer().AppMagic.AuthoringTool.Runtime.getNamedControl(getContainer().AppMagic.AuthoringTool.Runtime.getCurrentScreenName()).OpenAjax.uniqueId)
    }

    if (parentControlName) {
        var bindingContext = getContainer().AppMagic.Controls.GlobalContextManager.bindingContext.componentBindingContexts.lookup(parentControlName);
        var buttonWidget = bindingContext.controlContexts[controlName].controlWidget;
        var controlContext = buttonWidget.getOnSelectControlContext(bindingContext);
        buttonWidget.select(controlContext);
        return true;
    }
    return getContainer().AppMagic.Functions.select(null, getContainer().AppMagic.Controls.GlobalContextManager.bindingContext, getContainer().AppMagic.AuthoringTool.Runtime.getNamedControl(controlName), null, null, getContainer().AppMagic.AuthoringTool.Runtime.getNamedControl(getContainer().AppMagic.AuthoringTool.Runtime.getCurrentScreenName()).OpenAjax.uniqueId);
}

function setPropertyValueForControl(controlName, propertyName, value, parentControlName, rowOrColumnNumber) {
    if (parentControlName && rowOrColumnNumber) {
        var galleryControlOpenAjax = getContainer().AppMagic.AuthoringTool.Runtime.getNamedControl(parentControlName).OpenAjax;
        var replicatedContextManagerId = galleryControlOpenAjax.replicatedContextManager.managerId;
        var galleryBindingContext = galleryControlOpenAjax.replicatedContextManager.authoringAreaBindingContext.parent.replicatedContexts[replicatedContextManagerId].bindingContextAt(rowOrColumnNumber);
        return getContainer().AppMagic.AuthoringTool.Runtime.getNamedControl(controlName).OpenAjax.setPropertyValueInternal(propertyName, value, galleryBindingContext)
    }

    if (parentControlName) {

        return (getContainer().AppMagic.AuthoringTool.Runtime.getNamedControl(controlName).OpenAjax.setPropertyValueInternal(propertyName, value, getContainer().AppMagic.Controls.GlobalContextManager.bindingContext.componentBindingContexts.lookup(parentControlName)));
    }
    return getContainer().AppMagic.AuthoringTool.Runtime.getNamedControl(controlName, getContainer().AppMagic.Controls.GlobalContextManager.bindingContext).OpenAjax.setPropertyValueInternal(propertyName, value, getContainer().AppMagic.Controls.GlobalContextManager.bindingContext);
}

function isAppIdle() {

    if (window['UCWorkBlockTracker']) {
        return UCWorkBlockTracker.isAppIdle();
    }
    return true;
}