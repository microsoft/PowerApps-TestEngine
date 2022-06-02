// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


/* 
    This is code that handles communicating with the published app. Half of this code should live within the published app.
*/
var testEnginePluginName = "TestEnginePlugin";
var isPluginRegistered = false;

var callbackDictionary = {};

class TestEnginePlugin {
    contructor() {

    }

    processResult(commandInfo) {
        if (commandInfo.args[1].successful) {
            callbackDictionary[commandInfo.args[0]].complete(commandInfo.args[1].message);
        } else {
            callbackDictionary[commandInfo.args[0]].error(commandInfo.args[1].message);
        }
        delete callbackDictionary[commandInfo.args[0]];
    }
}

function executePublishedAppScript(scriptToExecute) {
    var callbackId = Core.Utility.generate128BitUUID();
    var completeablePromise = Core.Promise.createCompletablePromise();
    callbackDictionary[callbackId] = completeablePromise;
    AppMagic.Runtime.WebPlayerRuntime._appHostManager._apiHandler.invokeScriptAsync(`
        try {
            var result = ${scriptToExecute};
            Cordova.exec(function() {}, function() {}, '${testEnginePluginName}', 'processResult', ['${callbackId}', {successful: true, message: result}])
        }
        catch(err) {
            Cordova.exec(function() {}, function() {}, '${testEnginePluginName}', 'processResult', ['${callbackId}', {successful: false, message: err}])
        }`)
    return completeablePromise.promise;
}

function getOngoingActionsInPublishedApp() {
    return executePublishedAppScript("AppMagic.AuthoringTool.Runtime.existsOngoingAsync()");
}

function getControlObjectModel () {
    return executePublishedAppScript("buildControlObjectModel()");
}

function getPropertyValueFromPublishedApp(itemPath) {
    var script = `getPropertyValueFromControl(${JSON.stringify(itemPath)})`;
    return executePublishedAppScript(script);
}

function selectControl(itemPath) {
    var script = `selectControl(${JSON.stringify(itemPath)})`;
    return executePublishedAppScript(script);
}

function setPropertyValueForControl(itemPath, value) {
    var script = `setPropertyValueForControl(${JSON.stringify(itemPath)}, ${value})`;
    return executePublishedAppScript(script);
}

/*
 These are the functions that will be called by the Test Engine
*/

function getAppStatus() {
    if (typeof AppMagic === "undefined" || typeof AppMagic.Runtime === "undefined"
        || typeof AppMagic.Runtime.WebPlayerRuntime === "undefined" || typeof AppMagic.Runtime.WebPlayerRuntime._appHostManager === "undefined") {
        return "Loading";
    }
    if (AppMagic.Runtime.WebPlayerRuntime._appHostManager._appIsLoading) {
        return "Loading";
    }
    else {
        // Determine interaction required and error states
        
        // App is loaded, register plugin
        // When this is ported into PowerApps, need to do the proper plugin registration
        if (!isPluginRegistered) {
            AppMagic.Runtime.WebPlayerRuntime._appHostManager._apiHandler.registerHandler(testEnginePluginName, new TestEnginePlugin());
        }
        return getOngoingActionsInPublishedApp().then((ongoingAppActionRunning) => {
            if (ongoingAppActionRunning) {
                return "Busy";
            } else {
                return "Idle";
            }
        });
    }
}

function buildObjectModel() {
    return getControlObjectModel().then((controlObjectModel) => {
        return {
            controls: controlObjectModel
        };
    })
}

function getPropertyValue(itemPath) {
    return getPropertyValueFromPublishedApp(itemPath)
}


function select(itemPath) {
    return selectControl(itemPath)
}

function setPropertyValue(itemPath, value) {
    return setPropertyValueForControl(itemPath, value);
}