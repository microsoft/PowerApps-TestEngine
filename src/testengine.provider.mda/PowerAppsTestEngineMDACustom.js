// WARNING:
// The JavaScript object model of the page is subject to change. Do not take dependencies on the implementation of methods as they could be updated

class PowerAppsModelDrivenCanvas {
    static getAppMagic() {
        if (typeof AppDependencyHandler === "undefined" || typeof AppDependencyHandler.containers === "undefined") {
            return undefined;
        }
        var key = Object.keys(AppDependencyHandler.containers).filter(k => k != AppDependencyHandler.prebuiltContainerId && AppDependencyHandler.containers[k] != null)
        if (key.length == 0) {
            return undefined;
        }
        return AppDependencyHandler.containers[key[0]].AppMagic;
    }

    static executePublishedAppScript(scriptToExecute) {
        var promise = new Promise((resolve) => {
            var result = eval(scriptToExecute);
            resolve(result)
        });

        return promise;
    }

    static getOngoingActionsInPublishedApp() {
        return PowerAppsModelDrivenCanvas.executePublishedAppScript("AppDependencyHandler.containers[Object.keys(AppDependencyHandler.containers).filter(k => k != AppDependencyHandler.prebuiltContainerId)[0]].AppMagic.AuthoringTool.Runtime.existsOngoingAsync()");
    }

    static getControlObjectModel() {
        return PowerAppsModelDrivenCanvas.executePublishedAppScript("PowerAppsTestEngine.buildControlObjectModel()");
    }

    static getPropertyValueFromPublishedApp(itemPath) {
        var script = `PowerAppsModelDrivenCanvas.getPropertyValueFromControl(${JSON.stringify(itemPath)})`;
        return PowerAppsModelDrivenCanvas.executePublishedAppScript(script);
    }

    static getPropertyValueFromPublishedApp(itemPath) {
        var script = `PowerAppsModelDrivenCanvas.getPropertyValueFromControl(${JSON.stringify(itemPath)})`;
        return PowerAppsModelDrivenCanvas.executePublishedAppScript(script);
    }

    static selectControl(itemPath) {
        var script = `PowerAppsModelDrivenCanvas.selectControl(${JSON.stringify(itemPath)})`;
        return PowerAppsModelDrivenCanvas.executePublishedAppScript(script);
    }

    static interactWithControl(itemPath, value) {
        var script = "";
        if (isArray(Object.values(value))) {
            var valuesJsonArr = [];
            var values = Object.values(value);
            for (var index in values) {
                valuesJsonArr[`${index}`] = `${JSON.stringify(values[index])}`;
            }
            var valueJson = `{"${itemPath.propertyName}":${valuesJsonArr}}`;
            script = `PowerAppsModelDrivenCanvas.interactWithControl(${JSON.stringify(itemPath)}, ${valueJson})`;
        } else {
            var valueJson = `{"${itemPath.propertyName}":${value}}`;
            script = `PowerAppsModelDrivenCanvas.interactWithControl(${JSON.stringify(itemPath)}, ${valueJson})`;
        }
        return PowerAppsModelDrivenCanvas.executePublishedAppScript(script);
    }

    static setPropertyValueForControl(itemPath, value) {
        if (typeof value == "object") {
            return PowerAppsModelDrivenCanvas.interactWithControl(itemPath, value);
        } else if (typeof value == "string") {
            value = JSON.stringify(value);
        }
        var script = `PowerAppsModelDrivenCanvas.setPropertyValueForControl(${JSON.stringify(itemPath)}, ${value})`;
        return PowerAppsModelDrivenCanvas.executePublishedAppScript(script);
    }

    static fetchArrayItemCount(itemPath) {
        var script = `PowerAppsModelDrivenCanvas.fetchArrayItemCount(${JSON.stringify(itemPath)})`;
        return PowerAppsModelDrivenCanvas.executePublishedAppScript(script);
    }

    static isArray(obj) {
        return obj.constructor === Array;
    }

    static parseControl(controlName, controlObject) {
        var propertiesList = [];
        var properties = Object.keys(controlObject.modelProperties);
        var controls = [];
        if (controlObject.controlWidget.replicatedContextManager) {
            var childrenControlContext = controlObject.controlWidget.replicatedContextManager.authoringAreaBindingContext.controlContexts;
            var childControlNames = Object.keys(childrenControlContext);
            childControlNames.forEach((childControlName) => {
                var childControlObject = childrenControlContext[childControlName];
                var childControlsList = PowerAppsModelDrivenCanvas.parseControl(childControlName, childControlObject);
                controls = controls.concat(childControlsList);
            });
        }

        var appMagic = PowerAppsModelDrivenCanvas.getAppMagic()

        var componentBindingContext = appMagic.Controls.GlobalContextManager.bindingContext.componentBindingContexts.lookup(controlName);
        if (componentBindingContext) {
            var componentChildrenControlContext = componentBindingContext.controlContexts;
            var componentChildrenControlNames = Object.keys(componentChildrenControlContext);
            componentChildrenControlNames.forEach((childControlName) => {
                if (childControlName !== controlName) {
                    var componentChildControlObject = componentChildrenControlContext[childControlName];
                    var childControlsList = PowerAppsModelDrivenCanvas.parseControl(childControlName, componentChildControlObject);
                    controls = controls.concat(childControlsList);
                    propertiesList.push({ propertyName: childControlName, propertyType: childControlName });
                }
            });
        }

        properties.forEach((propertyName) => {
            var propertyType = controlObject.controlWidget.controlProperties[propertyName].propertyType;

            propertiesList.push({ propertyName: propertyName, propertyType: propertyType });
        })

        var control = { name: controlName, properties: propertiesList };
        controls.push(control);

        return controls;
    }

    static fetchArrayItemCount(itemPath) {
        if (itemPath.parentControl && itemPath.parentControl.index === null) {
            // Components do not have an item count
            throw "Not a gallery, no item count available. Most likely a component";
        }

        var appMagic = PowerAppsModelDrivenCanvas.getAppMagic();

        var value = appMagic.AuthoringTool.Runtime.getGlobalBindingContext().controlContexts[itemPath.controlName].modelProperties[itemPath.propertyName].getValue()

        return value.dataSource.data.Length;
    }

    static getBindingContext(itemPath) {
        var appMagic = PowerAppsModelDrivenCanvas.getAppMagic()

        var bindingContext = appMagic.Controls?.GlobalContextManager?.bindingContext;

        if (itemPath.parentControl) {
            // Control is inside a component or gallery
            bindingContext = PowerAppsModelDrivenCanvas.getBindingContext(itemPath.parentControl);
        }

        if (typeof itemPath.index !== 'undefined' && itemPath.index !== null) {
            // Gallery control
            var managerId = OpenAjax.widget.byId(itemPath.controlName).OpenAjax.getAuthoringControlContext()._replicatedContext.manager.managerId;
            return bindingContext.replicatedContexts[managerId].bindingContextAt(itemPath.index);
        }

        var componentBindingContext = appMagic.Controls.GlobalContextManager.bindingContext.componentBindingContexts.lookup(itemPath.controlName);

        if (typeof componentBindingContext !== 'undefined') {
            // Component control
            return componentBindingContext;
        }

        return bindingContext;
    }

    static getPropertyValueFromControl(itemPath) {
        var bindingContext = PowerAppsModelDrivenCanvas.getBindingContext(itemPath);

        var propertyValue = undefined

        var controlContext = bindingContext.controlContexts[itemPath.controlName];

        if (controlContext) {
            if (controlContext.modelProperties[itemPath.propertyName]) {
                propertyValue = controlContext.modelProperties[itemPath.propertyName]?.getValue();
            }
        }

        return {
            propertyValue: propertyValue
        };
    }

    static selectControl(itemPath) {
        var appMagic = PowerAppsModelDrivenCanvas.getAppMagic();

        var screenId = appMagic.AuthoringTool.Runtime.getNamedControl(appMagic.AuthoringTool.Runtime.getCurrentScreenName()).OpenAjax.uniqueId;

        if (itemPath.parentControl && itemPath.parentControl.index !== null) {
            // Gallery
            var bindingContext = appMagic.Controls.GlobalContextManager.bindingContext;
            if (itemPath.parentControl.parentControl) {
                // Nested gallery
                bindingContext = PowerAppsModelDrivenCanvas.getBindingContext(itemPath.parentControl);
                var currentControl = bindingContext.controlContexts[itemPath.controlName].controlWidget;
                return appMagic.Functions.select(null,
                    bindingContext,
                    currentControl.control,
                    null, // row number
                    null, // child control
                    screenId)
            }
            else {
                // One level gallery - the nested gallery approach doesn't work for one level so we have to do it differently
                // select function is starts with 1, while the C# code indexes from 0
                var rowOrColumnNumber = itemPath.parentControl.index + 1;
                return appMagic.Functions.select(null,
                    bindingContext,
                    appMagic.AuthoringTool.Runtime.getNamedControl(itemPath.parentControl.controlName),
                    rowOrColumnNumber,
                    appMagic.AuthoringTool.Runtime.getNamedControl(itemPath.controlName).OpenAjax._icontrol,
                    screenId)
            }
        }

        if (itemPath.parentControl) {
            // Component
            var bindingContext = appMagic.Controls.GlobalContextManager.bindingContext.componentBindingContexts.lookup(itemPath.parentControl.controlName);
            var buttonWidget = bindingContext.controlContexts[itemPath.controlName].controlWidget;
            var controlContext = buttonWidget.getOnSelectControlContext(bindingContext);
            buttonWidget.select(controlContext);
            return true;
        }

        return appMagic.Functions.select(null,
            appMagic.Controls.GlobalContextManager.bindingContext,
            appMagic.AuthoringTool.Runtime.getNamedControl(itemPath.controlName),
            null,
            null,
            screenId);
    }

    static setPropertyValueForControl(itemPath, value) {
        var appMagic = PowerAppsModelDrivenCanvas.getAppMagic()

        if (itemPath.parentControl && itemPath.parentControl.index !== null) {
            // Gallery & Nested gallery
            var galleryBindingContext = PowerAppsModelDrivenCanvas.getBindingContext(itemPath.parentControl);
            return appMagic.AuthoringTool.Runtime.getNamedControl(itemPath.controlName, galleryBindingContext).OpenAjax.setPropertyValueInternal(itemPath.propertyName, value, galleryBindingContext)
        }

        if (itemPath.parentControl) {
            // Component
            var componentBindingContext = appMagic.Controls.GlobalContextManager.bindingContext.componentBindingContexts.lookup(itemPath.parentControl.controlName);
            return (appMagic.AuthoringTool.Runtime.getNamedControl(itemPath.controlName, componentBindingContext).OpenAjax.setPropertyValueInternal(itemPath.propertyName, value, componentBindingContext));
        }

        return appMagic.AuthoringTool.Runtime.getNamedControl(itemPath.controlName, appMagic.Controls.GlobalContextManager.bindingContext).OpenAjax.setPropertyValueInternal(itemPath.propertyName, value, appMagic.Controls.GlobalContextManager.bindingContext);
    }

    static interactWithControl(itemPath, value) {
        var appMagic = PowerAppsModelDrivenCanvas.getAppMagic()

        var e = appMagic.AuthoringTool.Runtime.getNamedControl(itemPath.controlName, appMagic.Controls.GlobalContextManager.bindingContext).OpenAjax.interactWithControlAsync(appMagic.Controls.GlobalContextManager.bindingContext.controlContexts[itemPath.controlName], value).then(() => true, () => false);

        return e._value;
    }

    static buildControlObjectModel()
    {
        var appMagic = PowerAppsModelDrivenCanvas.getAppMagic()

        var controls = [];
        var controlContext = appMagic.Controls.GlobalContextManager.bindingContext.controlContexts;
        var controlNames = Object.keys(controlContext);
        controlNames.forEach((controlName) => {
            var control = controlContext[controlName];
            var controlList = PowerAppsModelDrivenCanvas.parseControl(controlName, control);
            controls = controls.concat(controlList);
        });

        return JSON.stringify({ Controls: controls });
    }

    static getControlProperties(itemPath)
    {
        var data = [];
        var appMagic = PowerAppsModelDrivenCanvas.getAppMagic()

        var controls = [];
        var controlContext = appMagic.Controls.GlobalContextManager.bindingContext.controlContexts;
        var controlNames = Object.keys(controlContext);
        controlNames.forEach((controlName) => {
            var control = controlContext[controlName];
            var controlList = PowerAppsModelDrivenCanvas.parseControl(controlName, control);
            controls = controls.concat(controlList);
        });

        var match = controls.filter(c => c.name == itemPath.controlName);
        if (match.length >= 0 && typeof match[0] == 'object') {
            match[0].properties.forEach((item) => {
                data.push(
                    {
                        Key: item.propertyName,
                        Value: PowerAppsModelDrivenCanvas.getPropertyValueFromControl({ controlName: itemPath.controlName, propertyName: item.propertyName })?.propertyValue
                    })
            })
        }
        return JSON.stringify(data);
    }
}