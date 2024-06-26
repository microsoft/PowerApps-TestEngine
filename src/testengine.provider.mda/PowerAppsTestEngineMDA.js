// WARNING:
// The JavaScript object model of the page is subject to change. Do not take dependencies on the implementation of methods as they could be updated 

class PowerAppsTestEngine {
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
            case 'entitylist':
                // TODO - Load list as collection
                break;
            case 'custom':
                // TODO - Load custom page
                break;
            case 'entityrecord':
                // TODO - Load Grid and Other control type

                // Reference
                // https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/controls
                return JSON.stringify({
                    Controls: Xrm.Page.ui.controls.getAll().map(item => {
                        var control = {};
                        control.Name = item.getName();
                        control.Properties = [];
                        control.Properties.push({ PropertyName: 'Disabled', PropertyType: 'b' });
                        control.Properties.push({ PropertyName: 'ShowLabel', PropertyType: 'b' });
                        control.Properties.push({ PropertyName: 'Label', PropertyType: 's' });
                        control.Properties.push({ PropertyName: 'Visible', PropertyType: 'b' });
                        control.Properties.push({ PropertyName: 'IsRequired', PropertyType: 'b' });
                        control.Properties.push({ PropertyName: 'Type', PropertyType: 's' });
                        // TODO Handle non standard controls

                        // Reference
                        // https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/controls/getcontroltype
                        switch (item.getControlType()) {
                            case 'standard':
                                control.Properties.push({ PropertyName: 'Text', PropertyType: 's' });
                                break;
                        }
                        return control;
                    })
                })
        }
    }

    static getValue(name) {
        switch (PowerAppsTestEngine.pageType()) {
            case 'entityrecord':
                // References:
                // https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/controls
                // https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/controls/getvalue
                return Xrm.Page.ui.controls.getByName(name).getValue();
        }
    }    

    static getControlProperties(name) {
        var data = [];
        switch (PowerAppsTestEngine.pageType()) {
            case 'entityrecord':
                // WARNING:
                // controlDescriptor JavaScript object is subject to change. Do not take dependencies on the object or properties returned as they could change without notice.
                var controlDescriptor = Xrm.Page.ui.controls.getByName(name).controlDescriptor;

                // Alternative: https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/controls/getdisabled
                data.push({ Key: 'Disabled', Value: controlDescriptor.Disabled.toString().toLowerCase() });
                data.push({ Key: 'ShowLabel', Value: controlDescriptor.ShowLabel.toString().toLowerCase() });
                if (controlDescriptor.ShowLabel) {
                    // Alternative https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/controls/getlabel
                    data.push({ Key: 'Label', Value: controlDescriptor.Label });
                }
                // Alternative: https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/controls/getvisible
                data.push({ Key: 'Visible', Value: controlDescriptor.Visible.toString().toLowerCase() });
                data.push({ Key: 'IsRequired', Value: controlDescriptor.IsRequired.toString().toLowerCase() });
                break;
        }
        return JSON.stringify(data);
    }
}