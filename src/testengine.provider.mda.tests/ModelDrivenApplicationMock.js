// Provide mock intereface for Model driven application for testing

var mockPageType = 'entityrecord'
// Other possible types
// - entitylist
// - custom
// - dashboard

// Set the mock value for the control
var mockValue = null;
var mockControlName = "test";
var mockControlType = "standard";

// Set the control descriptor properties
var mockControlDescriptor = class {
    static Disabled = false;
    static ShowLabel = true;
    static Label = "";
    static Visible = true;
    static IsRequired = false;
}

var mockControl = class {
    static value = null;
    static disabled = false;
    static label = '';
    static visible = true;
    static changed = false;

    static controlDescriptor = mockControlDescriptor;

    static getName() {
        return mockControlName;
    }

    static getValue() {
        return mockValue;
    }

    static getControlType() {
        return mockControlType;
    }

    static getAttribute() {
        return class {
            static setValue(data) {
                mockControl.value = data;
            }

            static fireOnChange() {
                mockControl.changed = true;
            }
        }
    }

    static setLabel(data) {
        mockControl.label = data;
    }

    static setDisabled(data) {
        mockControl.disabled = data;
    }

    static setVisible(data) {
        mockControl.visible = data;
    }
}

var mockControls = []
mockControls.push(mockControl)

class Xrm {
    static Page = class {
        static ui = class {
            static controls = class {
                static getAll() {
                    return mockControls;
                }

                static getByName(name) {
                    return mockControl;
                }
            }
        }
    }

    static Utility = class {
        static getPageContext() {
            return class {
                static input = class {
                    static pageType = mockPageType
                }
            }
        }
    }
}