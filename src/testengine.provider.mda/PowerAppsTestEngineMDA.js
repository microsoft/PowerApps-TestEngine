// WARNING:
// The JavaScript object model of the page is subject to change. Do not take dependencies on the implementation of methods as they could be updated
class PowerAppsTestEngine {
    static CONSTANTS = Object.freeze({
        EntityList: "entitylist",
        Custom: "custom",
        EntityRecord: "entityrecord",
        Dashboard: "dashboard"
    });

    static idleStatus() {
        // Reference:
        // https://github.com/microsoft/EasyRepro/blob/1935401875313a9059481d8af0a3708f66a3fe08/Microsoft.Dynamics365.UIAutomation.Browser/Extensions/SeleniumExtensions.cs#L347
        return UCWorkBlockTracker?.isAppIdle() ? 'Idle' : 'Loading'
    }

    static pageType() {
        var context = Xrm.Utility.getPageContext();
        if (typeof context?.input !== 'undefined') {
            return context.input.pageType
        }
        throw new Error('Unable to get page context');
    }

    static buildControlObjectModel() {
        switch (PowerAppsTestEngine.pageType()) {
            case PowerAppsTestEngine.CONSTANTS.EntityList:
                // TODO - Load list as collection
                break;
            case PowerAppsTestEngine.CONSTANTS.Custom:
                return PowerAppsModelDrivenCanvas.buildControlObjectModel();
            case PowerAppsTestEngine.CONSTANTS.EntityRecord:
                return PowerAppsModelDrivenEntityRecord.buildControlObjectModel();
        }
    }

    static getPropertyValue(itemPath) {
        if (typeof itemPath === 'string') {
            itemPath = JSON.parse(itemPath)
        }
        switch (PowerAppsTestEngine.pageType()) {
            case PowerAppsTestEngine.CONSTANTS.Custom:
                // TODO
                break;
        }
    }

    static getValue(name) {
        switch (PowerAppsTestEngine.pageType()) {
            case PowerAppsTestEngine.CONSTANTS.Custom:
                // TODO
                break;
            case PowerAppsTestEngine.CONSTANTS.EntityRecord:
                return PowerAppsModelDrivenEntityRecord.getValue(name);
        }
    }    

    static getControlProperties(itemPath) {
        var data = [];

        if (typeof itemPath === 'string') {
            itemPath = JSON.parse(itemPath)
        }

        switch (PowerAppsTestEngine.pageType()) {
            case PowerAppsTestEngine.CONSTANTS.Custom:
                return PowerAppsModelDrivenCanvas.getControlProperties(itemPath);
            case PowerAppsTestEngine.CONSTANTS.EntityRecord:
                return PowerAppsModelDrivenEntityRecord.getControlProperties(itemPath);
        }
        return JSON.stringify(data);
    }
    
    static setPropertyValue(item, data) {
        if (typeof item === 'string') {
            item = JSON.parse(item)
        }
        switch (PowerAppsTestEngine.pageType()) {
            case PowerAppsTestEngine.CONSTANTS.Custom:
                return PowerAppsModelDrivenCanvas.setPropertyValueForControl(item, data);
            case PowerAppsTestEngine.CONSTANTS.EntityRecord:
                return PowerAppsModelDrivenEntityRecord.setPropertyValueForControl(item, data);
        }
        return false;
    }
    
    static getItemCount(itemPath) {
        if (typeof itemPath === 'string') {
            itemPath = JSON.parse(itemPath)
        }
        switch (PowerAppsTestEngine.pageType()) {
            case PowerAppsTestEngine.CONSTANTS.Custom:
                return PowerAppsModelDrivenCanvas.fetchArrayItemCount(itemPath);
            case PowerAppsTestEngine.CONSTANTS.EntityList:
                return PowerAppsModelDrivenCanvas.fetchArrayItemCount(itemPath);
            case PowerAppsTestEngine.CONSTANTS.EntityRecord:
                // TODO - Get count of items for name
                break;
        }
    }

    static select(itemPath) {
        if (typeof itemPath === 'string') {
            itemPath = JSON.parse(itemPath)
        }
        switch (PowerAppsTestEngine.pageType()) {
            case PowerAppsTestEngine.CONSTANTS.Custom:
                return PowerAppsModelDrivenCanvas.selectControl(itemPath);
            case PowerAppsTestEngine.CONSTANTS.EntityRecord:
                // TODO - Selectitem
                break;
        }
    }
}
