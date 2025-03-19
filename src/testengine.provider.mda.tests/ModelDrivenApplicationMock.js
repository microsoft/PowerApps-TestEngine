// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

//----------------------------------------------------------------------------
// MDA - Entity Detail
//----------------------------------------------------------------------------

// Provide mock interface for Model driven application for testing

var mockPageType = 'entityrecord';

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
    static Label = "Text Input";
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

var mockLookup = class {
}

//----------------------------------------------------------------------------
// Custom Page Mocks
//----------------------------------------------------------------------------

var mockCanvasControl = class {
    static modelProperties = class {
        static X = class {
            static getValue() {
                return 1;
            }
        }

        static Y = class {
            static getValue() {
                return 2;
            }
        }

        static Text = class {
            static getValue() {
                return mockValue;
            }
        }
    }

    static controlWidget = class {
        static controlProperties = class {
            static X = class {
                static propertyType = 'n'
            }
            static Y = class {
                static propertyType = 'n'
            }
            static Text = class {
                static propertyType = 's'
            }
        }
    }
}

var mockAppMagic = class {

    static AuthoringTool = class {
        static Runtime = class {
            static getNamedControl(controlName, componentBindingContext) {
                return OpenAjaxWrapper;
            }
        } 
    }

    static Controls = class {
        static GlobalContextManager = class {
            static bindingContext = class {
                static controlContexts = class {
                    static TextInput1 = mockCanvasControl
                }

                static componentBindingContexts = class {
                    static lookup(controlName) {
                        return undefined;
                    }
                }
            }
        }
    }
}

var AppDependencyHandler = class {
    static prebuiltContainerId = ''

    static containers = class {
        static test = class {
            static AppMagic = mockAppMagic
        }
    }
}

var OpenAjaxClass = class {
    static uniqueId = ''
    static _icontrol = ''

    static getAuthoringControlContext() {
        return class {
            static _replicatedContext = null;
        }
    }

    static setPropertyValueInternal(propertyName, value, galleryBindingContext) {

    }

    static widget = class {
        static byId(name) {
            return OpenAjaxWrapper
        }
    }
}

var OpenAjaxWrapper = class {
    static OpenAjax = OpenAjaxClass
}

var OpenAjax = OpenAjaxClass

//----------------------------------------------------------------------------
// Entity List Mocks
//----------------------------------------------------------------------------

var mockColumns = class {
    static test = class {
        static DisplayName = "Test"
        static Type = "string"
    }
}

var mockRow = class {
    static getValue() {
        return 'value';
    }
}

var mockRows = [
    class {
        static data = class {
            static entity = class {
                static attributes = class {
                    static getAll() {
                        return [mockRow];
                    }
                }
            }
        }
    }
]

// https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/grids/grid
var mockGrid = class {
    // https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/grids/grid/gettotalrecordcount
    static getTotalRowCount() {
        return mockRows.length
    }

    // https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/grids/grid/getrows
    static getRows() {
        return class {
            static get(index) {
                return mockRows[index]
            }
        }
    }
}

var getCurrentXrmStatus = () => {
    return class {
        static mainGrid = class {
            static getColumnInfo() {
                return mockColumns
            }

            static getGrid() {
                return mockGrid;
            }
        }
    }
}