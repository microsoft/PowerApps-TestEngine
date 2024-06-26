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
            case 'custom':
                break;
            case 'entityrecord':
                // TODO: Handle multiple control types. For example lookup, grids

                // References:
                // https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/controls
                // https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/controls/getvalue
                return Xrm.Page.ui.controls.getByName(name).getValue();
        }
    }    

    static getControlProperties(name) {
        var data = [];
        switch (PowerAppsTestEngine.pageType()) {
            case 'custom':
                // TODO: Get custom page properties
                break;
            case 'entitylist':
                // TODO: Get Grid properties
                break;
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
    
    static setPropertyValue(item, data) {
        switch (PowerAppsTestEngine.pageType()) {
            case 'custom':
                // TODO set custom page value
                break;
            case 'entityrecord':
                var control = Xrm.Page.ui.controls.getByName(item.controlName);
                var propertyName = typeof item.propertyName === 'string' ? item.propertyName.toLowerCase() : ''
                // TODO - Set the Xrm SDK Value and update state for any JS to run
                // TODO: Handle Grid
                switch (propertyName) {
                    case '':
                    case 'text':
                        // https://learn.microsoft.com/power-apps/developer/model-driven-apps/clientapi/reference/controls/getattribute
                        var attribute = control.getAttribute();
                        // https://learn.microsoft.com/power-apps/developer/model-driven-apps/clientapi/reference/attributes/setvalue
                        // TODO: Handle different control types. For example lookup, options
                        attribute.setValue(data);
                        attribute.fireOnChange();
                        return true;
                    case 'label':
                        // https://learn.microsoft.com/power-apps/developer/model-driven-apps/clientapi/reference/controls/setlabel
                        control.setLabel(data);
                        return true;
                    case 'disabled':
                        // https://learn.microsoft.com/power-apps/developer/model-driven-apps/clientapi/reference/controls/setdisabled
                        control.setDisabled(data);
                        return true;
                    case 'visible':
                        // https://learn.microsoft.com/power-apps/developer/model-driven-apps/clientapi/reference/controls/setvisible
                        control.setVisible(data);
                        return true;
                }
                return false;
                break;
        }
        return false;
    }
    
    static getItemCount(item) {
        switch (PowerAppsTestEngine.pageType()) {
            case 'entityrecord':
                // TODO - Get count of items for name
                break;
        }
    }

    static select(item) {
        switch (PowerAppsTestEngine.pageType()) {
            case 'entityrecord':
                // TODO - Select item
                break;
        }
    }
}