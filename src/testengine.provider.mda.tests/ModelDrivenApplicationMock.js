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