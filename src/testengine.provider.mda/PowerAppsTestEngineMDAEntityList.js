// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

// WARNING:
// The JavaScript object model of the page is subject to change. Do not take dependencies on the implementation of methods as they could be updated

class PowerAppsModelDrivenEntityList {
    //implementation subject to change for Client side and entity interactions
    static CONSTANTS = Object.freeze({
        MainGrid: "Items",
        MainGridRowsRecordName: "Rows",
        EntityId: "entityId"
    });

    static getMainGridControls() {
        //TODO: set property types other than columns and individual column properties also
        // Warning: control object population for the main grid is only for rows currently
        var propertyTypeString = "*["
        propertyTypeString += "]";
        return JSON.stringify({ Controls: [{ Name: PowerAppsModelDrivenEntityList.CONSTANTS.MainGrid, Properties: [{ PropertyName: PowerAppsModelDrivenEntityList.CONSTANTS.MainGridRowsRecordName, PropertyType: propertyTypeString }] }] });
    }
}