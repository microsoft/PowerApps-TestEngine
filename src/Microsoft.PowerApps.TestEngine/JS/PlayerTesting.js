// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

function checkIfAppIsLoading() {
    return AppMagic.Runtime.WebPlayerRuntime._appHostManager._appIsLoading === true;
}

function getPublishedAppIframeName() {
    return "fullscreen-app-host";
}