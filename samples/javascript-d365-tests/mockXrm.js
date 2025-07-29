/**
 * mockXrm.js - Mock implementation of Dynamics 365 Xrm object for testing
 * This file provides a simple mock of the Xrm object to enable testing of client-side scripts
 */

// Persistent stores for attributes, controls, and sections
const attributeStore = {};
const controlStore = {};
const sectionStore = {};
const tabStore = {};

var Xrm = {
    Page: {
        ui: {
            formType: 2, // Default to Update form type
            tabs: {
                get: function(tabName) {
                    if (!tabStore[tabName]) {
                        tabStore[tabName] = {
                            visible: true,
                            setVisible: function(visible) { this.visible = visible; },
                            getVisible: function() { return this.visible; },
                            sections: {
                                get: function(sectionName) {
                                    if (!sectionStore[sectionName]) {
                                        sectionStore[sectionName] = {
                                            visible: true,
                                            setVisible: function(visible) { this.visible = visible; },
                                            getVisible: function() { return this.visible; }
                                        };
                                    }
                                    return sectionStore[sectionName];
                                }
                            }
                        };
                    }
                    return tabStore[tabName];
                }
            },
            controls: {}, // Not used, see getControl below
            getFormType: function() { return this.formType; },
            setFormType: function(type) { this.formType = type; }
        },

        data: {
            entity: {
                attributes: {}
            }
        },

        getAttribute: function(attributeName) {
            // Use persistent store
            if (!attributeStore[attributeName]) {
                attributeStore[attributeName] = {
                    value: null,
                    requiredLevel: "none",
                    handlers: [],
                    getValue: function() { return this.value; },
                    setValue: function(value) { 
                        this.value = value; 
                        this.handlers.forEach(function(handler) { handler(); });
                    },
                    setRequiredLevel: function(level) { this.requiredLevel = level; },
                    getRequiredLevel: function() { return this.requiredLevel; },
                    addOnChange: function(handler) { this.handlers.push(handler); }
                };
            }
            return attributeStore[attributeName];
        },

        getControl: function(controlName) {
            // Use persistent store
            if (!controlStore[controlName]) {
                controlStore[controlName] = {
                    visible: true,
                    notification: null,
                    setVisible: function(visible) { this.visible = visible; },
                    getVisible: function() { return this.visible; },
                    setNotification: function(message, id) { this.notification = { message: message, id: id }; },
                    clearNotification: function(id) { 
                        if (this.notification && this.notification.id === id) {
                            this.notification = null;
                        }
                    },
                    getNotification: function() { return this.notification; }
                };
            }
            return controlStore[controlName];
        }
    },

    Utility: {
        alertDialog: function(message, callback) {
            console.log("Alert Dialog: " + message);
            if (callback) callback();
            return true;
        },
        confirmDialog: function(message, callback) {
            console.log("Confirm Dialog: " + message);
            if (callback) callback(true); // Always confirm in test
            return true;
        }
    },

    WebApi: {
        online: true,
        execute: function(request) {
            console.log("WebApi.execute called with:", request);
            // Mock implementation returns a resolved promise
            return Promise.resolve({
                ok: true,
                json: function() {
                    return Promise.resolve({
                        value: "Mock API response"
                    });
                }
            });
        },
        retrieveMultipleRecords: function(entityName, options, maxPageSize) {
            console.log("WebApi.retrieveMultipleRecords called for:", entityName);
            // Mock implementation returns a resolved promise with empty result set
            return Promise.resolve({
                entities: []
            });
        },
        retrieveRecords: function(entityType, options) {
            console.log("WebApi.retrieveRecords called for:", entityType);
            return Promise.resolve({
                entities: []
            });
        }
    }
};

// Add attributes to support testing
Xrm.Page.getAttribute("accountstatus").setValue(1); // Default to active
Xrm.Page.getAttribute("activecases_count").setValue(0); // Default to no active cases
Xrm.Page.getAttribute("accounttype").setValue(0); // Default to no specific type
Xrm.Page.getAttribute("industrycode").setValue(0); // Default to no specific industry
Xrm.Page.getAttribute("creditlimit").setValue(5000); // Default credit limit
Xrm.Page.getAttribute("creditscore").setValue(650); // Default credit score
Xrm.Page.getAttribute("customertype").setValue(0); // Default customer type

// Create mock implementation for dialog display used in recommendations
window.showDialogResponse = true;
window.showModalDialog = function(url, args, options) {
    console.log("Show Modal Dialog called with:", args);
    return window.showDialogResponse;
};

// Functions for testing
function resetMockXrm() {
    Xrm.Page.ui.formType = 2;
    Xrm.Page.getAttribute("accountstatus").setValue(1);
    Xrm.Page.getAttribute("activecases_count").setValue(0);
    Xrm.Page.getAttribute("accounttype").setValue(0);
    Xrm.Page.getAttribute("industrycode").setValue(0);
    Xrm.Page.getAttribute("creditlimit").setValue(5000);
    Xrm.Page.getAttribute("creditscore").setValue(650);
    Xrm.Page.getAttribute("customertype").setValue(0);
    window.showDialogResponse = true;
}

// Add support for form context rather than just global form
Xrm.Page.context = {
    getClientUrl: function() {
        return "https://mock.crm.dynamics.com";
    }
};

// Add form event registration capability
Xrm.Page.data.entity.addOnSave = function(handler) {
    // Just store the handler - not implemented for tests
};
