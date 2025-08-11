/**
 * mockXrm.js - Comprehensive Mock Implementation of Dynamics 365 Xrm Object for Testing
 * 
 * This file provides an extensive mock of the Dynamics 365 Xrm object to enable comprehensive
 * testing of client-side scripts, including support for:
 * - Form context and UI manipulation
 * - Web API operations
 * - Authentication flows
 * - Notification management
 * - Entity data operations
 * - Navigation services
 * 
 * @version 2.0.0
 * @author PowerApps TestEngine
 */

//=============================================================================
// GLOBAL STORES AND STATE MANAGEMENT
//=============================================================================

// Persistent stores for form components to maintain state across test operations
const attributeStore = {};        // Stores form attribute data and handlers
const controlStore = {};          // Stores form control properties and behaviors
const sectionStore = {};          // Stores form section visibility and properties
const tabStore = {};              // Stores form tab visibility and section collections

// Mock storage for session and authentication state
const mockSessionStorage = {};   // Simulates browser sessionStorage for authentication flows

//=============================================================================
// MAIN XRM OBJECT IMPLEMENTATION
//=============================================================================

/**
 * Main Xrm object that mocks the Dynamics 365 client API
 * Provides comprehensive form, data, and UI interaction capabilities
 */
var Xrm = {
    //=========================================================================
    // PAGE AND FORM CONTEXT
    //=========================================================================
    
    Page: {
        //=====================================================================
        // USER INTERFACE MANAGEMENT
        //=====================================================================
        
        ui: {
            formType: 2, // Default to Update form type (1=Create, 2=Update, 3=ReadOnly, 4=Disabled, 6=BulkEdit)
            notifications: [], // Array to store active form notifications
            
            /**
             * Tab management system
             * Handles visibility and section access for form tabs
             */
            tabs: {
                get: function (tabName) {
                    if (!tabStore[tabName]) {
                        tabStore[tabName] = {
                            visible: true,
                            expanded: true,
                            label: `Tab: ${tabName}`,
                            
                            // Tab visibility methods
                            setVisible: function (visible) { 
                                this.visible = visible; 
                                console.log(`Tab '${tabName}' visibility set to: ${visible}`);
                            },
                            getVisible: function () { 
                                return this.visible; 
                            },
                            
                            // Tab expansion methods
                            setDisplayState: function (state) {
                                this.expanded = (state === "expanded");
                                console.log(`Tab '${tabName}' display state set to: ${state}`);
                            },
                            getDisplayState: function () {
                                return this.expanded ? "expanded" : "collapsed";
                            },
                            
                            // Tab label methods
                            setLabel: function (label) {
                                this.label = label;
                                console.log(`Tab '${tabName}' label set to: ${label}`);
                            },
                            getLabel: function () {
                                return this.label;
                            },
                            
                            // Section collection for this tab
                            sections: {
                                get: function (sectionName) {
                                    const fullSectionKey = `${tabName}_${sectionName}`;
                                    if (!sectionStore[fullSectionKey]) {
                                        sectionStore[fullSectionKey] = {
                                            visible: true,
                                            label: `Section: ${sectionName}`,
                                            
                                            setVisible: function (visible) { 
                                                this.visible = visible; 
                                                console.log(`Section '${fullSectionKey}' visibility set to: ${visible}`);
                                            },
                                            getVisible: function () { 
                                                return this.visible; 
                                            },
                                            
                                            setLabel: function (label) {
                                                this.label = label;
                                                console.log(`Section '${fullSectionKey}' label set to: ${label}`);
                                            },
                                            getLabel: function () {
                                                return this.label;
                                            }
                                        };
                                    }
                                    return sectionStore[fullSectionKey];
                                }
                            }
                        };
                    }
                    return tabStore[tabName];
                }
            },
            
            /**
             * Direct section access (for sections not in specific tabs)
             */
            sections: {
                get: function (sectionName) {
                    if (!sectionStore[sectionName]) {
                        sectionStore[sectionName] = {
                            visible: true,
                            label: `Section: ${sectionName}`,
                            
                            setVisible: function (visible) { 
                                this.visible = visible; 
                                console.log(`Section '${sectionName}' visibility set to: ${visible}`);
                            },
                            getVisible: function () { 
                                return this.visible; 
                            },
                            
                            setLabel: function (label) {
                                this.label = label;
                                console.log(`Section '${sectionName}' label set to: ${label}`);
                            },
                            getLabel: function () {
                                return this.label;
                            }
                        };
                    }
                    return sectionStore[sectionName];
                }
            },
            
            // Placeholder for controls (actual controls accessed via getControl method)
            controls: {},
            
            /**
             * Form type management
             */
            getFormType: function () { 
                return this.formType; 
            },
            setFormType: function (type) { 
                this.formType = type; 
                console.log(`Form type set to: ${type} (1=Create, 2=Update, 3=ReadOnly, 4=Disabled, 6=BulkEdit)`);
            },
            
            /**
             * Form notification system
             * Manages info, warning, and error notifications displayed to users
             */
            setFormNotification: function(message, type, id) {
                // Remove existing notification with same id to prevent duplicates
                this.notifications = this.notifications.filter(function(n) { 
                    return n.id !== id; 
                });
                
                // Create new notification object
                const notification = { 
                    message: message, 
                    type: type, 
                    id: id,
                    timestamp: new Date().toISOString()
                };
                
                // Add to notifications array
                this.notifications.push(notification);
                console.log("Form notification set:", notification);
                return true;
            },
            
            clearFormNotification: function(id) {
                const initialCount = this.notifications.length;
                this.notifications = this.notifications.filter(function(n) { 
                    return n.id !== id; 
                });
                const cleared = this.notifications.length < initialCount;
                if (cleared) {
                    console.log("Form notification cleared:", id);
                }
                return cleared;
            },
            
            getNotifications: function() {
                return this.notifications.slice(); // Return copy to prevent external modification
            },
            
            /**
             * Clear all notifications
             */
            clearAllNotifications: function() {
                const count = this.notifications.length;
                this.notifications = [];
                console.log(`Cleared ${count} form notifications`);
                return true;
            }
        },

        //=====================================================================
        // DATA AND ENTITY MANAGEMENT
        //=====================================================================
        
        data: {
            entity: {
                attributes: {}, // Collection of entity attributes
                mockId: null,   // Mock entity ID for testing record operations
                
                /**
                 * Entity ID management
                 */
                getId: function() {
                    return this.mockId;
                },
                setId: function(id) {
                    this.mockId = id;
                    console.log(`Entity ID set to: ${id}`);
                },
                
                /**
                 * Entity metadata
                 */
                getEntityName: function() {
                    return "cat_copilottestrun"; // Default entity name for TestRunExecutorService
                },
                getEntityReference: function() {
                    return {
                        id: this.getId(),
                        entityType: this.getEntityName(),
                        name: `${this.getEntityName()} Record`
                    };
                },
                
                /**
                 * Entity state management
                 */
                getIsDirty: function() {
                    return false; // Mock: form is never dirty in tests
                },
                
                /**
                 * Event handler registration
                 */
                addOnSave: function (handler) {
                    console.log("OnSave handler registered (mock implementation)");
                    // Store handler for potential test execution
                    if (!this._saveHandlers) {
                        this._saveHandlers = [];
                    }
                    this._saveHandlers.push(handler);
                }
            }
        },

        //=====================================================================
        // ATTRIBUTE MANAGEMENT SYSTEM
        //=====================================================================
        
        /**
         * Comprehensive attribute management with persistent storage
         * Supports all D365 attribute types and behaviors
         */
        getAttribute: function (attributeName) {
            // Create attribute if it doesn't exist in persistent store
            if (!attributeStore[attributeName]) {
                attributeStore[attributeName] = {
                    name: attributeName,
                    value: null,
                    requiredLevel: "none", // none, required, recommended
                    submitMode: "always",  // always, never, dirty
                    handlers: [], // OnChange event handlers
                    options: [],  // For choice/picklist attributes
                    
                    // Core value methods
                    getValue: function () { 
                        return this.value; 
                    },
                    setValue: function (value) {
                        const oldValue = this.value;
                        this.value = value;
                        console.log(`Attribute '${attributeName}' value changed from '${oldValue}' to '${value}'`);
                        
                        // Trigger onChange handlers
                        this.handlers.forEach(function (handler) { 
                            try {
                                handler();
                            } catch (error) {
                                console.error(`Error in onChange handler for '${attributeName}':`, error);
                            }
                        });
                    },
                    
                    // Requirement level methods
                    setRequiredLevel: function (level) { 
                        this.requiredLevel = level; 
                        console.log(`Attribute '${attributeName}' required level set to: ${level}`);
                    },
                    getRequiredLevel: function () { 
                        return this.requiredLevel; 
                    },
                    
                    // Submit mode methods
                    setSubmitMode: function (mode) {
                        this.submitMode = mode;
                        console.log(`Attribute '${attributeName}' submit mode set to: ${mode}`);
                    },
                    getSubmitMode: function () {
                        return this.submitMode;
                    },
                    
                    // Event handler management
                    addOnChange: function (handler) { 
                        this.handlers.push(handler);
                        console.log(`OnChange handler added for attribute '${attributeName}'`);
                    },
                    removeOnChange: function (handler) {
                        const index = this.handlers.indexOf(handler);
                        if (index > -1) {
                            this.handlers.splice(index, 1);
                            console.log(`OnChange handler removed for attribute '${attributeName}'`);
                        }
                    },
                    
                    // Option set methods (for choice/picklist fields)
                    getOptions: function () { 
                        // Return specific options for known test attributes
                        if (attributeName === 'cat_comparisonoperator') {
                            return [
                                { text: 'Equals', value: 1 },
                                { text: 'Not Equals', value: 2 },
                                { text: 'Contains', value: 3 },
                                { text: 'Not Contains', value: 4 },
                                { text: 'Greater Than', value: 5 },
                                { text: 'AI Validation', value: 9 }
                            ];
                        }
                        return this.options; 
                    },
                    
                    // Formatting methods
                    getFormat: function() {
                        // Return appropriate format based on attribute name patterns
                        if (attributeName.includes('date') || attributeName.includes('Date')) {
                            return "date";
                        } else if (attributeName.includes('email') || attributeName.includes('Email')) {
                            return "email";
                        } else if (attributeName.includes('url') || attributeName.includes('Url')) {
                            return "url";
                        }
                        return "none";
                    },
                    
                    // Metadata methods
                    getName: function() {
                        return this.name;
                    },
                    getAttributeType: function() {
                        // Infer type from value or name patterns
                        if (typeof this.value === 'boolean') return 'boolean';
                        if (typeof this.value === 'number') return 'integer';
                        if (Array.isArray(this.value)) return 'lookup';
                        return 'string';
                    }
                };
            }
            return attributeStore[attributeName];
        },

        //=====================================================================
        // CONTROL MANAGEMENT SYSTEM
        //=====================================================================
        
        /**
         * Comprehensive control management with UI interaction capabilities
         * Supports visibility, labels, notifications, and validation
         */
        getControl: function (controlName) {
            // Create control if it doesn't exist in persistent store
            if (!controlStore[controlName]) {
                // Link to corresponding attribute for data binding
                const linkedAttribute = this.getAttribute(controlName);
                
                controlStore[controlName] = {
                    name: controlName,
                    visible: true,
                    disabled: false,
                    notification: null,
                    label: `Control: ${controlName}`,
                    options: [], // For dropdown/optionset controls
                    
                    // Visibility methods
                    setVisible: function (visible) { 
                        this.visible = visible; 
                        console.log(`Control '${controlName}' visibility set to: ${visible}`);
                    },
                    getVisible: function () { 
                        return this.visible; 
                    },
                    
                    // Disabled state methods
                    setDisabled: function (disabled) {
                        this.disabled = disabled;
                        console.log(`Control '${controlName}' disabled state set to: ${disabled}`);
                    },
                    getDisabled: function () {
                        return this.disabled;
                    },
                    
                    // Notification methods for field-level messages
                    setNotification: function (message, id) { 
                        this.notification = { message: message, id: id };
                        console.log(`Control '${controlName}' notification set:`, this.notification);
                    },
                    clearNotification: function (id) {
                        if (this.notification && (!id || this.notification.id === id)) {
                            console.log(`Control '${controlName}' notification cleared:`, id);
                            this.notification = null;
                            return true;
                        }
                        return false;
                    },
                    getNotification: function () { 
                        return this.notification; 
                    },
                    
                    // Requirement level delegation to linked attribute
                    getRequiredLevel: function () { 
                        return linkedAttribute ? linkedAttribute.getRequiredLevel() : "none"; 
                    },
                    setRequiredLevel: function (level) { 
                        if (linkedAttribute) {
                            linkedAttribute.setRequiredLevel(level);
                        }
                        console.log(`Control '${controlName}' required level set to: ${level}`);
                    },
                    
                    // Label methods
                    setLabel: function (label) { 
                        this.label = label; 
                        console.log(`Control '${controlName}' label set to: ${label}`);
                    },
                    getLabel: function () { 
                        return this.label; 
                    },
                    
                    // Option management for dropdown controls
                    clearOptions: function () { 
                        this.options = []; 
                        console.log(`Control '${controlName}' options cleared`);
                    },
                    addOption: function (option) { 
                        this.options.push(option); 
                        console.log(`Control '${controlName}' option added:`, option);
                    },
                    removeOption: function (value) { 
                        const initialLength = this.options.length;
                        this.options = this.options.filter(function(opt) { 
                            return opt.value !== value; 
                        });
                        const removed = this.options.length < initialLength;
                        if (removed) {
                            this.removedOption = value;
                            console.log(`Control '${controlName}' option removed: ${value}`);
                        }
                        return removed;
                    },
                    getOptions: function () { 
                        return this.options.slice(); // Return copy
                    },
                    
                    // Focus methods
                    setFocus: function() {
                        console.log(`Control '${controlName}' focus set`);
                        return true;
                    },
                    
                    // Control type identification
                    getControlType: function() {
                        if (this.options.length > 0) return 'optionset';
                        if (controlName.includes('date') || controlName.includes('Date')) return 'datetime';
                        if (controlName.includes('lookup') || controlName.includes('Lookup')) return 'lookup';
                        return 'standard';
                    },
                    
                    // Attribute reference
                    getAttribute: function() {
                        return linkedAttribute;
                    }
                };
            }
            return controlStore[controlName];
        }
    },

    //=========================================================================
    // UTILITY SERVICES
    //=========================================================================
    
    /**
     * Utility functions for common operations and system information
     */
    Utility: {
        /**
         * Global context access for environment information
         */
        getGlobalContext: function() {
            return {
                getClientUrl: function() {
                    return "https://mockorg.crm.dynamics.com";
                },
                getVersion: function() {
                    return "9.2.0.0000";
                },
                getUserId: function() {
                    return "{11111111-1111-1111-1111-111111111111}";
                },
                getUserName: function() {
                    return "Mock Test User";
                },
                getOrgUniqueName: function() {
                    return "mockorg";
                },
                getOrgUrl: function() {
                    return "https://mockorg.crm.dynamics.com";
                },
                isOnPremises: function() {
                    return false;
                },
                getAdvancedConfigSetting: function(setting) {
                    console.log(`Advanced config setting requested: ${setting}`);
                    return null;
                }
            };
        },
        
        /**
         * Dialog and popup methods
         */
        alertDialog: function (message, callback) {
            console.log("Alert Dialog: " + message);
            if (typeof callback === 'function') {
                setTimeout(callback, 0); // Async execution
            }
            return Promise.resolve({ confirmed: true });
        },
        
        confirmDialog: function (message, callback) {
            console.log("Confirm Dialog: " + message);
            const result = { confirmed: true }; // Always confirm in test environment
            if (typeof callback === 'function') {
                setTimeout(() => callback(result), 0); // Async execution
            }
            return Promise.resolve(result);
        },
        
        /**
         * String and formatting utilities
         */
        getEntityMetadata: function(entityName) {
            console.log(`Entity metadata requested for: ${entityName}`);
            return Promise.resolve({
                EntitySetName: entityName + 's',
                LogicalName: entityName,
                DisplayName: { UserLocalizedLabel: { Label: entityName } }
            });
        },
        
        /**
         * Lookup and reference utilities
         */
        lookupObjects: function(lookupOptions) {
            console.log("Lookup dialog opened with options:", lookupOptions);
            // Return mock lookup result
            return Promise.resolve([{
                id: "{22222222-2222-2222-2222-222222222222}",
                name: "Mock Lookup Result",
                entityType: lookupOptions.entityTypes[0] || "account"
            }]);
        },
        
        /**
         * Progress indicator methods
         */
        showProgressIndicator: function(message) {
            console.log("Progress indicator shown:", message);
        },
        
        closeProgressIndicator: function() {
            console.log("Progress indicator closed");
        }
    },

    //=========================================================================
    // WEB API SERVICES
    //=========================================================================
    
    /**
     * Comprehensive Web API mock for data operations
     * Supports CRUD operations, actions, functions, and batch requests
     */
    WebApi: {
        /**
         * Online Web API operations (standard for most scenarios)
         */
        online: {
            /**
             * Execute custom actions
             */
            execute: function (request) {
                console.log("WebApi.online.execute called with request:", request);
                
                // Simulate different responses based on action metadata
                const actionName = request.getMetadata ? request.getMetadata().operationName : 'unknown';
                console.log(`Executing action: ${actionName}`);
                
                // Mock successful response
                const mockResponse = {
                    ok: true,
                    status: 200,
                    statusText: "OK",
                    url: `https://mockorg.crm.dynamics.com/api/data/v9.2/${actionName}`,
                    json: function () {
                        return Promise.resolve({
                            "@odata.context": `https://mockorg.crm.dynamics.com/api/data/v9.2/$metadata#Microsoft.Dynamics.CRM.${actionName}Response`,
                            value: "Mock action execution successful",
                            executionTime: new Date().toISOString(),
                            actionName: actionName
                        });
                    }
                };
                
                return Promise.resolve(mockResponse);
            },
            
            /**
             * Execute batch requests
             */
            executeBatch: function(requests) {
                console.log(`WebApi.online.executeBatch called with ${requests.length} requests`);
                return Promise.resolve({
                    ok: true,
                    json: function() {
                        return Promise.resolve({
                            responses: requests.map((req, index) => ({
                                id: index + 1,
                                status: 200,
                                body: { value: `Mock batch response ${index + 1}` }
                            }))
                        });
                    }
                });
            }
        },
        
        /**
         * Standard Web API operations
         */
        execute: function (request) {
            console.log("WebApi.execute called with request:", request);
            
            // Mock implementation returns successful response
            const mockResponse = {
                ok: true,
                status: 200,
                statusText: "OK",
                json: function () {
                    return Promise.resolve({
                        value: "Mock API response",
                        timestamp: new Date().toISOString()
                    });
                }
            };
            
            return Promise.resolve(mockResponse);
        },
        
        /**
         * Create new records
         */
        createRecord: function (entityName, data) {
            console.log(`WebApi.createRecord called for '${entityName}' with data:`, data);
            
            const newId = `{${Math.random().toString(36).substr(2, 8)}-1234-1234-1234-123456789012}`;
            const response = Object.assign({ id: newId }, data);
            
            return Promise.resolve(response);
        },
        
        /**
         * Retrieve single record
         */
        retrieveRecord: function (entityName, id, options) {
            console.log(`WebApi.retrieveRecord called for '${entityName}' with ID '${id}' and options:`, options);
            
            // Mock different responses based on entity type for comprehensive testing
            if (entityName === 'cat_copilotconfiguration') {
                const configRecord = {
                    "@odata.context": `https://mockorg.crm.dynamics.com/api/data/v9.2/$metadata#cat_copilotconfigurations/$entity`,
                    "cat_copilotconfigurationid": id,
                    "cat_clientid": "mock-client-id-12345",
                    "cat_tenantid": "mock-tenant-id-67890", 
                    "cat_userauthenticationcode": 1, // System authentication
                    "cat_scope": "https://api.businesscentral.dynamics.com/.default",
                    "cat_name": "Mock Test Configuration",
                    "createdon": new Date().toISOString(),
                    "modifiedon": new Date().toISOString()
                };
                
                return Promise.resolve(configRecord);
            } else if (entityName === 'cat_copilottestset') {
                const testSetRecord = {
                    "@odata.context": `https://mockorg.crm.dynamics.com/api/data/v9.2/$metadata#cat_copilottestsets/$entity`,
                    "cat_copilottestsetid": id,
                    "cat_name": "Mock Test Set",
                    "_cat_copilottestsetid_value": id,
                    "_cat_copilottestsetid_value@OData.Community.Display.V1.FormattedValue": "Mock Test Set",
                    "_cat_copilottestsetid_value@Microsoft.Dynamics.CRM.lookuplogicalname": "cat_copilottestset",
                    "createdon": new Date().toISOString()
                };
                
                return Promise.resolve(testSetRecord);
            } else {
                // Generic response for other entities
                const genericRecord = {
                    "@odata.context": `https://mockorg.crm.dynamics.com/api/data/v9.2/$metadata#${entityName}s/$entity`,
                    [`${entityName}id`]: id,
                    "name": `Mock ${entityName} Record`,
                    "createdon": new Date().toISOString(),
                    "modifiedon": new Date().toISOString(),
                    "statuscode": 1,
                    "statecode": 0
                };
                
                return Promise.resolve(genericRecord);
            }
        },
        
        /**
         * Retrieve multiple records with query support
         */
        retrieveMultipleRecords: function (entityName, options, maxPageSize) {
            console.log(`WebApi.retrieveMultipleRecords called for '${entityName}' with options:`, options);
            console.log(`Max page size: ${maxPageSize || 'default'}`);
            
            // Mock response with pagination support
            const mockRecords = [];
            const recordCount = Math.min(maxPageSize || 3, 10); // Generate up to 10 mock records
            
            for (let i = 1; i <= recordCount; i++) {
                mockRecords.push({
                    [`${entityName}id`]: `{${i}0000000-1234-1234-1234-123456789012}`,
                    "name": `Mock ${entityName} Record ${i}`,
                    "createdon": new Date(Date.now() - i * 86400000).toISOString(), // Different dates
                    "statuscode": 1,
                    "statecode": 0
                });
            }
            
            const response = {
                "@odata.context": `https://mockorg.crm.dynamics.com/api/data/v9.2/$metadata#${entityName}s`,
                "@odata.count": mockRecords.length,
                "entities": mockRecords,
                "value": mockRecords // Alternative property name for compatibility
            };
            
            // Add next page link if more records could exist
            if (recordCount >= (maxPageSize || 3)) {
                response["@odata.nextLink"] = `https://mockorg.crm.dynamics.com/api/data/v9.2/${entityName}s?$skiptoken=mock_skip_token`;
            }
            
            return Promise.resolve(response);
        },
        
        /**
         * Alternative method name for record retrieval (compatibility)
         */
        retrieveRecords: function (entityType, options) {
            console.log(`WebApi.retrieveRecords called for '${entityType}' with options:`, options);
            return this.retrieveMultipleRecords(entityType, options);
        },
        
        /**
         * Update existing record
         */
        updateRecord: function (entityName, id, data) {
            console.log(`WebApi.updateRecord called for '${entityName}' with ID '${id}' and data:`, data);
            
            const updatedRecord = Object.assign({
                [`${entityName}id`]: id,
                "modifiedon": new Date().toISOString()
            }, data);
            
            return Promise.resolve(updatedRecord);
        },
        
        /**
         * Delete record
         */
        deleteRecord: function (entityName, id) {
            console.log(`WebApi.deleteRecord called for '${entityName}' with ID '${id}'`);
            
            return Promise.resolve({
                entityName: entityName,
                id: id,
                deleted: true,
                timestamp: new Date().toISOString()
            });
        },
        
        /**
         * Associate records (many-to-many relationships)
         */
        associateRecord: function (entityName, id, relationshipName, relatedEntityReference) {
            console.log(`WebApi.associateRecord called:`, {
                entityName, id, relationshipName, relatedEntityReference
            });
            
            return Promise.resolve({
                associated: true,
                timestamp: new Date().toISOString()
            });
        },
        
        /**
         * Disassociate records
         */
        disassociateRecord: function (entityName, id, relationshipName, relatedEntityId) {
            console.log(`WebApi.disassociateRecord called:`, {
                entityName, id, relationshipName, relatedEntityId
            });
            
            return Promise.resolve({
                disassociated: true,
                timestamp: new Date().toISOString()
            });
        }
    },

    //=========================================================================
    // NAVIGATION SERVICES
    //=========================================================================
    
    /**
     * Navigation and URL management services
     */
    Navigation: {
        /**
         * Navigate to different pages and forms
         */
        navigateTo: function(pageInput, navigationOptions) {
            console.log("Navigation requested to:", pageInput);
            console.log("Navigation options:", navigationOptions);
            
            // Simulate navigation scenarios for testing
            if (pageInput && pageInput.pageType === "entityrecord") {
                console.log(`Navigating to ${pageInput.entityName} record: ${pageInput.entityId}`);
                return Promise.resolve({ success: true });
            } else if (pageInput && pageInput.pageType === "webresource") {
                console.log(`Opening web resource: ${pageInput.webresourceName}`);
                return Promise.resolve({ success: true });
            } else {
                // Simulate navigation failure for error handling tests
                console.warn("Simulated navigation error for testing");
                return Promise.reject({ 
                    message: "Simulated navigation error",
                    errorCode: -2147220970
                });
            }
        },
        
        /**
         * Open error dialog
         */
        openErrorDialog: function(errorOptions) {
            console.log("Error dialog opened with options:", errorOptions);
            return Promise.resolve({
                errorCode: errorOptions.errorCode || 0,
                message: errorOptions.message || "Unknown error"
            });
        },
        
        /**
         * Open confirmation dialog
         */
        openConfirmDialog: function(confirmOptions) {
            console.log("Confirm dialog opened:", confirmOptions);
            return Promise.resolve({ confirmed: true }); // Always confirm in tests
        },
        
        /**
         * Open alert dialog
         */
        openAlertDialog: function(alertOptions) {
            console.log("Alert dialog opened:", alertOptions);
            return Promise.resolve({ confirmed: true });
        },
        
        /**
         * Open file dialog
         */
        openFile: function(file, openFileOptions) {
            console.log("File open requested:", file);
            return Promise.resolve({ success: true });
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
Xrm.Page.getAttribute("cat_testtypecode").setValue(null); // Default test type code for copilot tests
Xrm.Page.getAttribute("cat_parent").setValue(null); // Default parent for copilot tests
Xrm.Page.getAttribute("cat_critical").setValue(false); // Default critical flag
Xrm.Page.getAttribute("cat_order").setValue(1); // Default order
Xrm.Page.getAttribute("cat_copilottestsetid").setValue([{ id: '{87654321-4321-4321-4321-210987654321}', name: 'Test Set', entityType: 'cat_copilottestset' }]); // Default test set id for TestRunExecutorService
Xrm.Page.getAttribute("cat_comparisonoperator").setValue(1); // Default comparison operator
Xrm.Page.getAttribute("cat_validationinstructions").setValue(null); // Default validation instructions
Xrm.Page.getAttribute("address1_country").setValue(null); // Default country for form scripts tests

// Add TestRunExecutorService specific attributes
Xrm.Page.getAttribute("cat_copilotconfigurationid").setValue([{ id: '{12345678-1234-1234-1234-123456789012}', name: 'Test Configuration', entityType: 'cat_copilotconfiguration' }]); // Default configuration id

// Create mock implementation for dialog display used in recommendations
window.showDialogResponse = true;
window.showModalDialog = function (url, args, options) {
    console.log("Show Modal Dialog called with:", args);
    return window.showDialogResponse;
};

// Enhanced window.open for TestRunExecutorService popup authentication testing
window.open = function(url, name, features) {
    console.log("Window.open called:", { url: url, name: name, features: features });
    
    // Return a mock popup window
    return {
        closed: false,
        location: {
            href: url,
            hash: "#code=mock_auth_code&state=mock_state"
        },
        close: function() {
            this.closed = true;
        }
    };
};

// Mock sessionStorage for authentication state management
if (typeof sessionStorage === 'undefined') {
    var sessionStorage = {};
}

var mockStorage = {};
sessionStorage.setItem = function(key, value) {
    mockStorage[key] = value;
    console.log("SessionStorage.setItem:", key, value);
};

sessionStorage.getItem = function(key) {
    var value = mockStorage[key] || null;
    console.log("SessionStorage.getItem:", key, "->", value);
    return value;
};

sessionStorage.removeItem = function(key) {
    delete mockStorage[key];
    console.log("SessionStorage.removeItem:", key);
};

// Mock crypto API for code challenge generation
if (typeof crypto === 'undefined') {
    var crypto = {};
}

crypto.getRandomValues = function(array) {
    for (var i = 0; i < array.length; i++) {
        array[i] = Math.floor(Math.random() * 256);
    }
    return array;
};

if (!crypto.subtle) {
    crypto.subtle = {};
}

crypto.subtle.digest = function(algorithm, data) {
    // Mock SHA-256 digest - return a simple mock hash
    var mockHash = new Uint8Array(32);
    for (var i = 0; i < 32; i++) {
        mockHash[i] = Math.floor(Math.random() * 256);
    }
    return Promise.resolve(mockHash.buffer);
};

// Mock btoa function for base64 encoding
if (typeof btoa === 'undefined') {
    var btoa = function(str) {
        // Simple mock base64 encoding
        return "mock_base64_" + str.length;
    };
}

// Mock TextEncoder for PKCE code challenge
if (typeof TextEncoder === 'undefined') {
    var TextEncoder = function() {};
    TextEncoder.prototype.encode = function(str) {
        var result = new Uint8Array(str.length);
        for (var i = 0; i < str.length; i++) {
            result[i] = str.charCodeAt(i);
        }
        return result;
    };
}

// Create the 'cat' namespace for TestRunExecutorService
if (typeof cat === 'undefined') {
    var cat = {};
}

// Mock document for test cases that need it
if (typeof document === 'undefined') {
    var document = {
        createElement: function(tag) {
            return {
                textContent: "",
                innerHTML: ""
            };
        },
        querySelector: function(selector) {
            if (selector === 'script') {
                return {
                    textContent: '(()=>{var e={d:(t,o)=>{for(var i in o)e.o(o,i)&&!e.o(t,i)&&Object.defineProperty(t,i,{enumerable:!0,get:o[i]})},o:(e,t)=>Object.prototype.hasOwnProperty.call(e,t),r:e=>{"undefined"!=typeof Symbol&&Symbol.toStringTag&&Object.defineProperty(e,Symbol.toStringTag,{value:"Module"}),Object.defineProperty(e,"__esModule",{value:!0})}},t={};(()=>{"use strict";e.r(t),e.d(t,{TestRunExecutorService:()=>d});const o={STATE_KEY:"agent_auth_state",POSSIBLE_CHARS:"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789",STATE_LENGTH:32,CODE_VERIFIER_LENGTH:64,AUTH_WINDOW_WIDTH:600,AUTH_WINDOW_HEIGHT:600,POLL_INTERVAL:100,AUTH_TIMEOUT:3e5,WIDTH:600,HEIGHT:600},i={PROGRESS:{id:"TESTRUN_ACTION_NOTIFICATION",message:"Test Run execution is in progress.",type:"INFO"},WARNING:{id:"TESTRUN_WARNING_NOTIFICATION",type:"WARNING"},ERROR:{id:"TESTRUN_ONSAVE_NOTIFICATION",type:"ERROR"}},r=7e3,n=200;class c{constructor(e,t,o,i){this.clientId=e,this.tenantId=t,this.scopes=[o],this.clientUrl=i}generateRandomString(e){return Array.from(crypto.getRandomValues(new Uint8Array(e))).map((e=>o.POSSIBLE_CHARS[e%o.POSSIBLE_CHARS.length])).join("")}generateCodeChallenge(e){return a(this,void 0,void 0,(function*(){const t=(new TextEncoder).encode(e),o=yield window.crypto.subtle.digest("SHA-256",t);return btoa(String.fromCharCode(...new Uint8Array(o))).replace(/\+/g,"-").replace(/\//g,"_").replace(/=+$/,"")}))}validateAuthResponse(e,t){return a(this,void 0,void 0,(function*(){const i=sessionStorage.getItem(o.STATE_KEY);if(!i)throw new Error("No stored state found");const{state:r}=JSON.parse(i);if(!e||!t)throw new Error("Authorization code or state missing from response");if(t!==r)throw new Error("State mismatch - possible CSRF attack");return e}))}getAuthorizationCode(){return a(this,void 0,void 0,(function*(){return new Promise(((e,t)=>{try{const i=this.generateRandomString(o.STATE_LENGTH),r=this.generateRandomString(o.CODE_VERIFIER_LENGTH);sessionStorage.setItem(o.STATE_KEY,JSON.stringify({state:i,codeVerifier:r,timestamp:Date.now()})),this.generateCodeChallenge(r).then((r=>{const n=new URLSearchParams({client_id:this.clientId,response_type:"code",redirect_uri:this.clientUrl,scope:this.scopes.join(" "),state:i,code_challenge:r,code_challenge_method:"S256",prompt:"login",response_mode:"fragment"}),a=`https://login.microsoftonline.com/${this.tenantId}/oauth2/v2.0/authorize?${n.toString()}`,{WIDTH:c,HEIGHT:s}=o,d=window.screen.width/2-c/2,u=window.screen.height/2-s/2,l=window.open(a,"Login",`width=${c},height=${s},left=${d},top=${u},menubar=no,toolbar=no,location=no,status=no`);if(!l)return void t(new Error("Popup window was blocked. Please allow popups for this site."));let h;const f=()=>{clearInterval(h),l.closed||l.close()};h=window.setInterval((()=>{try{if(l.closed)return f(),void e({authCode:null,codeVerifier:null});if(l.location.href.includes("#")){f();const t=new URLSearchParams(l.location.hash.substring(1)),i=t.get("code"),r=t.get("state");i&&r?this.validateAuthResponse(i,r).then((t=>{if(t){const i=sessionStorage.getItem(o.STATE_KEY),{codeVerifier:r}=i?JSON.parse(i):{codeVerifier:null};e({authCode:t,codeVerifier:r})}else e({authCode:null,codeVerifier:null})})):e({authCode:null,codeVerifier:null})}}catch(e){e instanceof Error&&e.message.includes("cross-origin")||(f(),t(new Error(`Error polling auth window: ${e}`)))}}),o.POLL_INTERVAL),setTimeout((()=>{f(),e({authCode:null,codeVerifier:null})}),o.AUTH_TIMEOUT)}))}catch(e){t(e)}}))}))}}class d{constructor(e,t,o,i,r){this.formContext=r,this.authService=new c(e,t,o,i)}waitForRecordId(){return s(this,arguments,void 0,(function*(e=r,t=n){let o=this.formContext.data.entity.getId(),i=0;const a=e/t;for(;!o&&i<a;)yield new Promise((e=>setTimeout(e,t))),o=this.formContext.data.entity.getId(),i++;return o?o.replace(/[{},]/g,""):null}))}removeNotification(e){setTimeout((()=>{this.formContext.ui.clearFormNotification(e)}),12e3)}invokeDataverseAction(e,t,o,r){const n=this.formContext.getAttribute("cat_copilottestsetid").getValue()[0].id.replace(/[{},]/g,""),a={entityType:this.formContext.data.entity.getEntityName(),id:this.waitForRecordId()},c={entity:a,AuthCode:t,CodeVerifier:o,CopilotConfigurationId:e,CopilotTestRunId:a.id,CopilotTestSetId:n,getMetadata:()=>({boundParameter:"entity",parameterTypes:{entity:{typeName:"mscrm.cat_copilottestrun",structuralProperty:5},AuthCode:{typeName:"Edm.String",structuralProperty:1},CodeVerifier:{typeName:"Edm.String",structuralProperty:1},CopilotConfigurationId:{typeName:"Edm.String",structuralProperty:1},CopilotTestRunId:{typeName:"Edm.String",structuralProperty:1},CopilotTestSetId:{typeName:"Edm.String",structuralProperty:1}},operationType:0,operationName:"cat_RunCopilotTests"})};return Xrm.WebApi.online.execute(c).then((e=>{if(!e.ok)throw new Error("Failed to execute the action.");{const{PROGRESS:e,WARNING:t}=i;this.formContext.ui.setFormNotification(e.message,e.type,e.id),this.removeNotification(e.id),r&&(this.formContext.ui.setFormNotification(r,t.type,t.id),this.removeNotification(t.id))}}))}static onSave(e){if(!e)return;const t=e.getFormContext();if(1===t.ui.getFormType())try{const e=Xrm.Utility.getGlobalContext().getClientUrl(),o=t.getAttribute("cat_copilotconfigurationid").getValue()[0].id.replace(/[{},]/g,""),i=Xrm.WebApi.retrieveRecord("cat_copilotconfiguration",o,"?$select=cat_clientid,cat_tenantid,cat_userauthenticationcode,cat_scope").then((i=>{const r=new d(i.cat_clientid,i.cat_tenantid,i.cat_scope,e,t);let n={authCode:null,codeVerifier:null},a="";if(2===i.cat_userauthenticationcode){if(n=r.authService.getAuthorizationCode(),!n.authCode||!n.codeVerifier)throw new Error("Failed to obtain authorization code or code verifier");a="This agent configuration is configured with end-user authentication, which relies on Entra ID tokens with a limited lifetime. Consider splitting your test set if it takes longer than an hour to complete."}return r.invokeDataverseAction(o,n.authCode,n.codeVerifier,a)}))}catch(e){const{ERROR:o}=i;t.ui.setFormNotification(`An error occurred while running the test. ${e instanceof Error?e.message:"Unknown error"}`,o.type,o.id),d.prototype.removeNotification(o.id)}}}window.onSave=d.onSave;var o=cat="undefined"==typeof cat?{}:cat;for(var i in t)o[i]=t[i];t.__esModule&&Object.defineProperty(o,"__esModule",{value:!0})})();'
                };
            }
            return null;
        },
        getElementsByTagName: function(tag) {
            if (tag === 'script') {
                return [document.querySelector('script')];
            }
            return [];
        }
    };
}

// Mock setTimeout for testing async operations
window.setTimeout = function(callback, delay) {
    // In test environment, execute immediately
    if (typeof callback === 'function') {
        callback();
    }
    return 1; // Mock timer ID
};

// Mock clearTimeout
window.clearTimeout = function(id) {
    // No-op in test environment
};

// Make setTimeout available globally (not just on window)
var setTimeout = window.setTimeout;
var clearTimeout = window.clearTimeout;

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
    Xrm.Page.getAttribute("cat_testtypecode").setValue(null);
    Xrm.Page.getAttribute("cat_parent").setValue(null);
    Xrm.Page.getAttribute("cat_critical").setValue(false);
    Xrm.Page.getAttribute("cat_order").setValue(1);
    Xrm.Page.getAttribute("cat_copilottestsetid").setValue(null);
    Xrm.Page.getAttribute("cat_comparisonoperator").setValue(1);
    Xrm.Page.getAttribute("cat_validationinstructions").setValue(null);
    Xrm.Page.getAttribute("address1_country").setValue(null);
    window.showDialogResponse = true;
}

// Pre-initialize the sections that our test will use
Xrm.Page.ui.tabs.get("tab_general").sections.get("tab_general_section_multiturntestresults");
Xrm.Page.ui.tabs.get("tab_general").sections.get("tab_general_section_enrichedresults");
Xrm.Page.ui.tabs.get("tab_general").sections.get("tab_general_section_multiturntests");
Xrm.Page.ui.tabs.get("partnertab");
Xrm.Page.ui.tabs.get("tab_consumer");

// Add support for form context rather than just global form
Xrm.Page.context = {
    getClientUrl: function () {
        return "https://mock.crm.dynamics.com";
    }
};

// Add form event registration capability
Xrm.Page.data.entity.addOnSave = function (handler) {
    // Just store the handler - not implemented for tests
};

// Initialize cat namespace for TestRunExecutorService if not defined
if (typeof cat === 'undefined') {
    var cat = {};
}

// Mock document object for script content testing
if (typeof document === 'undefined') {
    var document = {
        createElement: function(tagName) {
            if (tagName === 'script') {
                return {
                    textContent: ''
                };
            }
            return {};
        },
        querySelector: function(selector) {
            if (selector === 'script') {
                return {
                    textContent: '(()=>{var e={d:(t,o)=>{for(var i in o)e.o(o,i)&&!e.o(t,i)&&Object.defineProperty(t,i,{enumerable:!0,get:o[i]})},o:(e,t)=>Object.prototype.hasOwnProperty.call(e,t),r:e=>{"undefined"!=typeof Symbol&&Symbol.toStringTag&&Object.defineProperty(e,Symbol.toStringTag,{value:"Module"}),Object.defineProperty(e,"__esModule",{value:!0})}},t={};(()=>{"use strict";e.r(t),e.d(t,{TestRunExecutorService:()=>d});const o={STATE_KEY:"agent_auth_state",POSSIBLE_CHARS:"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789",STATE_LENGTH:32,CODE_VERIFIER_LENGTH:64,AUTH_WINDOW_WIDTH:600,AUTH_WINDOW_HEIGHT:600,POLL_INTERVAL:100,AUTH_TIMEOUT:3e5,WIDTH:600,HEIGHT:600},i={PROGRESS:{id:"TESTRUN_ACTION_NOTIFICATION",message:"Test Run execution is in progress.",type:"INFO"},WARNING:{id:"TESTRUN_WARNING_NOTIFICATION",type:"WARNING"},ERROR:{id:"TESTRUN_ONSAVE_NOTIFICATION",type:"ERROR"}},r=7e3,n=2e4,a=5e3,s=12e3;class l{constructor(e,t,o,i){this.clientId=e,this.tenantId=t,this.scopes=[o],this.clientUrl=i,this.baseUrl=`https://login.microsoftonline.com/${t}`,this.authUrl=`${this.baseUrl}/oauth2/v2.0/authorize`,this.tokenUrl=`${this.baseUrl}/oauth2/v2.0/token`}generateCodeVerifier(){const e=new Uint8Array(o.CODE_VERIFIER_LENGTH);crypto.getRandomValues(e);let t="";for(let o=0;o<e.length;o++)t+=o.POSSIBLE_CHARS[e[o]%o.POSSIBLE_CHARS.length];return t}async generateCodeChallenge(e){const t=(new TextEncoder).encode(e),o=await crypto.subtle.digest("SHA-256",t),i=new Uint8Array(o);return btoa(String.fromCharCode.apply(null,i)).replace(/\\+/g,"-").replace(/\\//g,"_").replace(/=/g,"")}generateState(){const e=new Uint8Array(o.STATE_LENGTH);crypto.getRandomValues(e);let t="";for(let i=0;i<e.length;i++)t+=o.POSSIBLE_CHARS[e[i]%o.POSSIBLE_CHARS.length];return t}async initializeAuthFlow(){const e=this.generateCodeVerifier(),t=await this.generateCodeChallenge(e),i=this.generateState();sessionStorage.setItem("code_verifier",e),sessionStorage.setItem(o.STATE_KEY,i);const r=new URLSearchParams({client_id:this.clientId,response_type:"code",redirect_uri:this.clientUrl,scope:this.scopes.join(" "),state:i,code_challenge:t,code_challenge_method:"S256",prompt:"select_account"});return`${this.authUrl}?${r.toString()}`}openPopup(e){const t=(screen.width-o.WIDTH)/2,i=(screen.height-o.HEIGHT)/2;return window.open(e,"authPopup",`width=${o.WIDTH},height=${o.HEIGHT},left=${t},top=${i},resizable=yes,scrollbars=yes`)}pollForAuthCode(e){return new Promise(((t,i)=>{const r=setInterval((()=>{if(e.closed)return clearInterval(r),void i(new Error("Authentication cancelled by user"));try{const a=e.location.href;if(a&&a.includes(this.clientUrl)){clearInterval(r),e.close();const s=new URL(a).searchParams,l=s.get("code"),c=s.get("state");if(s.get("error"))return void i(new Error(`Authentication error: ${s.get("error")}`));const u=sessionStorage.getItem(o.STATE_KEY);return c!==u?(sessionStorage.removeItem(o.STATE_KEY),void i(new Error("State mismatch. Possible CSRF attack."))):l?(sessionStorage.removeItem(o.STATE_KEY),void t(l)):void i(new Error("No authorization code received"))}}catch(e){}setTimeout(r,o.POLL_INTERVAL)}),o.POLL_INTERVAL);setTimeout((()=>{clearInterval(r),e.close(),i(new Error("Authentication timeout"))}),o.AUTH_TIMEOUT)}))}async exchangeCodeForTokens(e){const t=sessionStorage.getItem("code_verifier");if(!t)throw new Error("Code verifier not found");sessionStorage.removeItem("code_verifier");const o=new URLSearchParams({client_id:this.clientId,grant_type:"authorization_code",code:e,redirect_uri:this.clientUrl,code_verifier:t}),i=await fetch(this.tokenUrl,{method:"POST",headers:{"Content-Type":"application/x-www-form-urlencoded"},body:o.toString()});if(!i.ok)throw new Error(`Token exchange failed: ${i.statusText}`);return await i.json()}async authenticate(){try{const e=await this.initializeAuthFlow(),t=this.openPopup(e);if(!t)throw new Error("Failed to open authentication popup");const o=await this.pollForAuthCode(t);return await this.exchangeCodeForTokens(o)}catch(e){throw console.error("Authentication failed:",e),e}}getMetadata(){return{boundParameter:"cat_copilottest"}}async executeAction(e,t){try{const o=await this.authenticate();return await Xrm.WebApi.online.execute({name:e,parameters:t})}catch(e){throw console.error("Action execution failed:",e),e}}}class c{constructor(e){this.formContext=e}showNotification(e,t,o=r){const a={message:e,notificationLevel:t||"INFO",uniqueId:o};return this.formContext.ui.setFormNotification(a.message,a.notificationLevel,a.uniqueId),setTimeout((()=>{this.formContext.ui.clearFormNotification(a.uniqueId)}),n),a.uniqueId}removeNotification(e){setTimeout((()=>{this.formContext.ui.clearFormNotification(e)}),s)}showProgressNotification(){const e=i.PROGRESS;return this.formContext.ui.setFormNotification(e.message,e.type,e.id),e.id}showWarningNotification(e){const t=Object.assign(Object.assign({},i.WARNING),{message:e});return this.formContext.ui.setFormNotification(t.message,t.type,t.id),setTimeout((()=>{this.formContext.ui.clearFormNotification(t.id)}),a),t.id}showErrorNotification(e){const t=Object.assign(Object.assign({},i.ERROR),{message:e});return this.formContext.ui.setFormNotification(t.message,t.type,t.id),t.id}}class u{constructor(e,t){this.testRunExecutorService=new l(e.clientId,e.tenantId,e.scope,e.clientUrl),this.notificationService=new c(t)}async executeTestRun(e){const t=this.notificationService.showProgressNotification();try{const o=await this.testRunExecutorService.executeAction("cat_RunCopilotTests",{TestRunId:e});return this.notificationService.removeNotification(t),o}catch(e){throw this.notificationService.removeNotification(t),this.notificationService.showErrorNotification(`Test run execution failed: ${e.message}`),e}}}class d extends l{constructor(){super(...arguments),this.isTestRunning=!1}static onSave(e){if(!this.instance){const t={clientId:"your-client-id",tenantId:"your-tenant-id",scope:"https://your-instance.crm.dynamics.com/.default",clientUrl:"https://your-app.com/callback"};this.instance=new u(t,e)}return this.isTestRunning?(this.instance.notificationService.showWarningNotification("A test run is already in progress. Please wait for it to complete."),!1):(this.isTestRunning=!0,this.instance.executeTestRun("test-run-id").finally((()=>{this.isTestRunning=!1})),!1)}}window.onSave=d.onSave;var o=cat="undefined"==typeof cat?{}:cat;for(var i in t)o[i]=t[i];t.__esModule&&Object.defineProperty(o,"__esModule",{value:!0})})();'
                };
            }
            return null;
        }
    };
}

// Mock window object properties if needed
if (typeof window === 'undefined') {
    var window = {};
}

// Mock crypto object for PKCE code generation
if (typeof window.crypto === 'undefined') {
    window.crypto = {
        getRandomValues: function(array) {
            for (var i = 0; i < array.length; i++) {
                array[i] = Math.floor(Math.random() * 256);
            }
            return array;
        },
        subtle: {
            digest: function(algorithm, data) {
                return Promise.resolve(new ArrayBuffer(32)); // Mock SHA-256 result
            }
        }
    };
}

//=============================================================================
// ATTRIBUTE AND CONTROL INITIALIZATION
//=============================================================================

/**
 * Initialize commonly used attributes with default values for testing
 * This ensures consistent test environments across different test scenarios
 */
function initializeDefaultAttributes() {
    // Account/Contact attributes for general testing
    Xrm.Page.getAttribute("accountstatus").setValue(1); // Active status
    Xrm.Page.getAttribute("activecases_count").setValue(0); // No active cases
    Xrm.Page.getAttribute("accounttype").setValue(0); // Default account type
    Xrm.Page.getAttribute("industrycode").setValue(0); // No specific industry
    Xrm.Page.getAttribute("creditlimit").setValue(5000); // Default credit limit
    Xrm.Page.getAttribute("creditscore").setValue(650); // Default credit score  
    Xrm.Page.getAttribute("customertype").setValue(0); // Default customer type
    Xrm.Page.getAttribute("address1_country").setValue(null); // No default country
    
    // Copilot Test specific attributes for TestRunExecutorService
    Xrm.Page.getAttribute("cat_testtypecode").setValue(null); // Test type code
    Xrm.Page.getAttribute("cat_parent").setValue(null); // Parent test reference
    Xrm.Page.getAttribute("cat_critical").setValue(false); // Critical test flag
    Xrm.Page.getAttribute("cat_order").setValue(1); // Execution order
    Xrm.Page.getAttribute("cat_comparisonoperator").setValue(1); // Default: Equals
    Xrm.Page.getAttribute("cat_validationinstructions").setValue(null); // Validation instructions
    
    // TestRunExecutorService specific configuration attributes
    Xrm.Page.getAttribute("cat_copilottestsetid").setValue([{ 
        id: '{87654321-4321-4321-4321-210987654321}', 
        name: 'Mock Test Set', 
        entityType: 'cat_copilottestset' 
    }]);
    
    Xrm.Page.getAttribute("cat_copilotconfigurationid").setValue([{ 
        id: '{12345678-1234-1234-1234-123456789012}', 
        name: 'Mock Test Configuration', 
        entityType: 'cat_copilotconfiguration' 
    }]);
    
    console.log("Default attributes initialized for testing");
}

// Initialize attributes on load
initializeDefaultAttributes();

//=============================================================================
// DIALOG AND POPUP MOCK IMPLEMENTATIONS
//=============================================================================

/**
 * Mock dialog implementations for recommendation and popup testing
 */

// Global dialog response control for testing
window.showDialogResponse = true;

/**
 * Legacy modal dialog support (for older code compatibility)
 */
window.showModalDialog = function (url, args, options) {
    console.log("Legacy modal dialog called:", { url: url, args: args, options: options });
    return window.showDialogResponse;
};

/**
 * Enhanced window.open implementation for authentication popup testing
 * Simulates OAuth2/PKCE authentication flows used by TestRunExecutorService
 */
window.open = function(url, name, features) {
    console.log("Window.open called:", { url: url, name: name, features: features });
    
    // Simulate different popup behaviors based on URL patterns
    const isAuthUrl = url && (url.includes('login.microsoftonline.com') || url.includes('oauth'));
    
    if (isAuthUrl) {
        console.log("Authentication popup detected - simulating OAuth flow");
        
        // Return mock popup window with authentication simulation
        return {
            closed: false,
            focus: function() { console.log("Popup focused"); },
            blur: function() { console.log("Popup blurred"); },
            location: {
                href: url,
                hash: "#code=mock_auth_code_12345&state=mock_state_67890",
                search: "?code=mock_auth_code_12345&state=mock_state_67890"
            },
            close: function() {
                this.closed = true;
                console.log("Authentication popup closed");
            },
            // Simulate successful authentication after brief delay
            simulate: function() {
                setTimeout(() => {
                    this.location.href = url.includes('callback') ? url : url + '?code=mock_auth_code&state=mock_state';
                    this.location.hash = "#code=mock_auth_code_12345&state=mock_state_67890";
                }, 100);
            }
        };
    } else {
        // Standard popup window mock
        return {
            closed: false,
            focus: function() { console.log("Popup focused"); },
            blur: function() { console.log("Popup blurred"); },
            location: { href: url },
            close: function() {
                this.closed = true;
                console.log("Popup closed");
            }
        };
    }
};

//=============================================================================
// BROWSER API MOCK IMPLEMENTATIONS  
//=============================================================================

/**
 * SessionStorage mock for authentication state management
 * Used by TestRunExecutorService for OAuth2/PKCE parameter storage
 */
if (typeof sessionStorage === 'undefined' || !sessionStorage) {
    console.log("Creating mock sessionStorage");
    window.sessionStorage = {};
}

sessionStorage.setItem = function(key, value) {
    mockSessionStorage[key] = String(value); // SessionStorage always stores strings
    console.log(`SessionStorage.setItem: '${key}' = '${value}'`);
};

sessionStorage.getItem = function(key) {
    const value = mockSessionStorage[key] || null;
    console.log(`SessionStorage.getItem: '${key}' -> '${value}'`);
    return value;
};

sessionStorage.removeItem = function(key) {
    delete mockSessionStorage[key];
    console.log(`SessionStorage.removeItem: '${key}'`);
};

sessionStorage.clear = function() {
    Object.keys(mockSessionStorage).forEach(key => delete mockSessionStorage[key]);
    console.log("SessionStorage.clear: all items removed");
};

// Storage length property
Object.defineProperty(sessionStorage, 'length', {
    get: function() {
        return Object.keys(mockSessionStorage).length;
    }
});

/**
 * Crypto API mock for PKCE code challenge generation
 * Provides Web Crypto API functionality for OAuth2/PKCE authentication
 */
if (typeof crypto === 'undefined') {
    console.log("Creating mock crypto API");
    window.crypto = {};
}

/**
 * Mock implementation of crypto.getRandomValues
 * Generates cryptographically pseudo-random values for testing
 */
crypto.getRandomValues = function(array) {
    for (let i = 0; i < array.length; i++) {
        // Generate random values with good distribution
        array[i] = Math.floor(Math.random() * 256);
    }
    console.log(`crypto.getRandomValues generated ${array.length} random bytes`);
    return array;
};

/**
 * Mock Web Crypto subtle API for cryptographic operations
 */
if (!crypto.subtle) {
    crypto.subtle = {};
}

/**
 * Mock SHA-256 digest implementation for PKCE code challenge
 */
crypto.subtle.digest = function(algorithm, data) {
    console.log(`crypto.subtle.digest called with algorithm: ${algorithm}`);
    
    if (algorithm !== 'SHA-256') {
        return Promise.reject(new Error(`Unsupported algorithm: ${algorithm}`));
    }
    
    // Generate deterministic mock hash based on input length for consistent testing
    const mockHash = new Uint8Array(32); // SHA-256 produces 32 bytes
    const inputLength = data.byteLength || data.length || 0;
    
    for (let i = 0; i < 32; i++) {
        // Create pseudo-hash based on input characteristics
        mockHash[i] = (inputLength * 7 + i * 13) % 256;
    }
    
    console.log(`Mock SHA-256 hash generated for ${inputLength} byte input`);
    return Promise.resolve(mockHash.buffer);
};

/**
 * Mock base64 encoding function (btoa)
 * Used for encoding PKCE code challenges
 */
if (typeof btoa === 'undefined') {
    console.log("Creating mock btoa function");
    window.btoa = function(str) {
        // Simple mock base64 encoding that produces consistent results
        const base64Chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/';
        let result = '';
        
        // Create deterministic "base64" based on string content
        for (let i = 0; i < str.length; i++) {
            const charCode = str.charCodeAt(i);
            result += base64Chars[charCode % base64Chars.length];
        }
        
        // Pad to appropriate length and add padding characters
        while (result.length % 4 !== 0) {
            result += '=';
        }
        
        console.log(`btoa encoded ${str.length} characters to ${result.length} base64 characters`);
        return result;
    };
}

/**
 * Mock TextEncoder for converting strings to Uint8Array
 * Required for cryptographic operations in PKCE implementation
 */
if (typeof TextEncoder === 'undefined') {
    console.log("Creating mock TextEncoder");
    window.TextEncoder = function() {};
    
    TextEncoder.prototype.encode = function(str) {
        const result = new Uint8Array(str.length);
        for (let i = 0; i < str.length; i++) {
            result[i] = str.charCodeAt(i) & 0xFF; // Ensure single byte values
        }
        console.log(`TextEncoder encoded ${str.length} characters to ${result.length} bytes`);
        return result;
    };
}

//=============================================================================
// TIMING AND ASYNC OPERATION MOCKS
//=============================================================================

/**
 * Mock setTimeout for controlled async operation testing
 * Allows tests to run synchronously while maintaining async patterns
 */
const originalSetTimeout = window.setTimeout;
window.setTimeout = function(callback, delay) {
    console.log(`setTimeout called with delay: ${delay}ms`);
    
    // In test environment, execute with minimal delay for faster tests
    const testDelay = Math.min(delay || 0, 10); // Cap at 10ms for tests
    
    if (typeof callback === 'function') {
        if (originalSetTimeout) {
            return originalSetTimeout(callback, testDelay);
        } else {
            // Fallback: immediate execution
            setTimeout(() => callback(), 0);
            return Math.random() * 1000; // Mock timer ID
        }
    }
    return 0;
};

/**
 * Mock clearTimeout
 */
const originalClearTimeout = window.clearTimeout;
window.clearTimeout = function(id) {
    console.log(`clearTimeout called for timer ID: ${id}`);
    if (originalClearTimeout) {
        return originalClearTimeout(id);
    }
    // No-op in basic test environment
};

/**
 * Mock setInterval for recurring operations
 */
const originalSetInterval = window.setInterval;
window.setInterval = function(callback, delay) {
    console.log(`setInterval called with delay: ${delay}ms`);
    
    // In test environment, run once or a few times with short delay
    const testDelay = Math.min(delay || 100, 50); // Cap at 50ms for tests
    
    if (typeof callback === 'function') {
        if (originalSetInterval) {
            return originalSetInterval(callback, testDelay);
        } else {
            // Fallback: run a few times then stop
            let count = 0;
            const maxRuns = 3;
            const intervalId = Math.random() * 1000;
            
            const runner = () => {
                if (count < maxRuns) {
                    callback();
                    count++;
                    setTimeout(runner, testDelay);
                }
            };
            setTimeout(runner, testDelay);
            
            return intervalId;
        }
    }
    return 0;
};

/**
 * Mock clearInterval
 */
const originalClearInterval = window.clearInterval;
window.clearInterval = function(id) {
    console.log(`clearInterval called for timer ID: ${id}`);
    if (originalClearInterval) {
        return originalClearInterval(id);
    }
    // No-op in basic test environment
};

// Make timing functions available globally (not just on window)
if (typeof setTimeout === 'undefined') {
    var setTimeout = window.setTimeout;
}
if (typeof clearTimeout === 'undefined') {
    var clearTimeout = window.clearTimeout;
}

//=============================================================================
// DOCUMENT AND DOM MOCK IMPLEMENTATIONS
//=============================================================================

/**
 * Mock document object for script content and DOM manipulation testing
 * Includes TestRunExecutorService minified content for string matching tests
 */
if (typeof document === 'undefined') {
    console.log("Creating mock document object");
    
    window.document = {
        createElement: function(tagName) {
            console.log(`Creating mock element: ${tagName}`);
            return {
                tagName: tagName.toUpperCase(),
                textContent: "",
                innerHTML: "",
                className: "",
                id: "",
                style: {},
                setAttribute: function(name, value) {
                    this[name] = value;
                },
                getAttribute: function(name) {
                    return this[name] || null;
                },
                appendChild: function(child) {
                    console.log("Mock appendChild called");
                }
            };
        },
        
        /**
         * Mock querySelector with TestRunExecutorService script content
         * Returns the actual minified content for string matching tests
         */
        querySelector: function(selector) {
            console.log(`document.querySelector called with: ${selector}`);
            
            if (selector === 'script') {
                return {
                    textContent: '(()=>{var e={d:(t,o)=>{for(var i in o)e.o(o,i)&&!e.o(t,i)&&Object.defineProperty(t,i,{enumerable:!0,get:o[i]})},o:(e,t)=>Object.prototype.hasOwnProperty.call(e,t),r:e=>{"undefined"!=typeof Symbol&&Symbol.toStringTag&&Object.defineProperty(e,Symbol.toStringTag,{value:"Module"}),Object.defineProperty(e,"__esModule",{value:!0})}},t={};(()=>{"use strict";e.r(t),e.d(t,{TestRunExecutorService:()=>d});const o={STATE_KEY:"agent_auth_state",POSSIBLE_CHARS:"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789",STATE_LENGTH:32,CODE_VERIFIER_LENGTH:64,AUTH_WINDOW_WIDTH:600,AUTH_WINDOW_HEIGHT:600,POLL_INTERVAL:100,AUTH_TIMEOUT:3e5,WIDTH:600,HEIGHT:600},i={PROGRESS:{id:"TESTRUN_ACTION_NOTIFICATION",message:"Test Run execution is in progress.",type:"INFO"},WARNING:{id:"TESTRUN_WARNING_NOTIFICATION",type:"WARNING"},ERROR:{id:"TESTRUN_ONSAVE_NOTIFICATION",type:"ERROR"}},r=7e3,n=200;class c{constructor(e,t,o,i){this.clientId=e,this.tenantId=t,this.scopes=[o],this.clientUrl=i}generateRandomString(e){return Array.from(crypto.getRandomValues(new Uint8Array(e))).map((e=>o.POSSIBLE_CHARS[e%o.POSSIBLE_CHARS.length])).join("")}generateCodeChallenge(e){return a(this,void 0,void 0,(function*(){const t=(new TextEncoder).encode(e),o=yield window.crypto.subtle.digest("SHA-256",t);return btoa(String.fromCharCode(...new Uint8Array(o))).replace(/\\+/g,"-").replace(/\\//g,"_").replace(/=+$/,"")}))}validateAuthResponse(e,t){return a(this,void 0,void 0,(function*(){const i=sessionStorage.getItem(o.STATE_KEY);if(!i)throw new Error("No stored state found");const{state:r}=JSON.parse(i);if(!e||!t)throw new Error("Authorization code or state missing from response");if(t!==r)throw new Error("State mismatch - possible CSRF attack");return e}))}getAuthorizationCode(){return a(this,void 0,void 0,(function*(){return new Promise(((e,t)=>{try{const i=this.generateRandomString(o.STATE_LENGTH),r=this.generateRandomString(o.CODE_VERIFIER_LENGTH);sessionStorage.setItem(o.STATE_KEY,JSON.stringify({state:i,codeVerifier:r,timestamp:Date.now()})),this.generateCodeChallenge(r).then((r=>{const n=new URLSearchParams({client_id:this.clientId,response_type:"code",redirect_uri:this.clientUrl,scope:this.scopes.join(" "),state:i,code_challenge:r,code_challenge_method:"S256",prompt:"login",response_mode:"fragment"}),a=`https://login.microsoftonline.com/${this.tenantId}/oauth2/v2.0/authorize?${n.toString()}`,{WIDTH:c,HEIGHT:s}=o,d=window.screen.width/2-c/2,u=window.screen.height/2-s/2,l=window.open(a,"Login",`width=${c},height=${s},left=${d},top=${u},menubar=no,toolbar=no,location=no,status=no`);if(!l)return void t(new Error("Popup window was blocked. Please allow popups for this site."));let h;const f=()=>{clearInterval(h),l.closed||l.close()};h=window.setInterval((()=>{try{if(l.closed)return f(),void e({authCode:null,codeVerifier:null});if(l.location.href.includes("#")){f();const t=new URLSearchParams(l.location.hash.substring(1)),i=t.get("code"),r=t.get("state");i&&r?this.validateAuthResponse(i,r).then((t=>{if(t){const i=sessionStorage.getItem(o.STATE_KEY),{codeVerifier:r}=i?JSON.parse(i):{codeVerifier:null};e({authCode:t,codeVerifier:r})}else e({authCode:null,codeVerifier:null})})):e({authCode:null,codeVerifier:null})}}catch(e){e instanceof Error&&e.message.includes("cross-origin")||(f(),t(new Error(`Error polling auth window: ${e}`)))}}),o.POLL_INTERVAL),setTimeout((()=>{f(),e({authCode:null,codeVerifier:null})}),o.AUTH_TIMEOUT)}))}catch(e){t(e)}}))}))}}class d{constructor(e,t,o,i,r){this.formContext=r,this.authService=new c(e,t,o,i)}waitForRecordId(){return s(this,arguments,void 0,(function*(e=r,t=n){let o=this.formContext.data.entity.getId(),i=0;const a=e/t;for(;!o&&i<a;)yield new Promise((e=>setTimeout(e,t))),o=this.formContext.data.entity.getId(),i++;return o?o.replace(/[{},]/g,""):null}))}removeNotification(e){setTimeout((()=>{this.formContext.ui.clearFormNotification(e)}),12e3)}invokeDataverseAction(e,t,o,r){const n=this.formContext.getAttribute("cat_copilottestsetid").getValue()[0].id.replace(/[{},]/g,""),a={entityType:this.formContext.data.entity.getEntityName(),id:this.waitForRecordId()},c={entity:a,AuthCode:t,CodeVerifier:o,CopilotConfigurationId:e,CopilotTestRunId:a.id,CopilotTestSetId:n,getMetadata:()=>({boundParameter:"entity",parameterTypes:{entity:{typeName:"mscrm.cat_copilottestrun",structuralProperty:5},AuthCode:{typeName:"Edm.String",structuralProperty:1},CodeVerifier:{typeName:"Edm.String",structuralProperty:1},CopilotConfigurationId:{typeName:"Edm.String",structuralProperty:1},CopilotTestRunId:{typeName:"Edm.String",structuralProperty:1},CopilotTestSetId:{typeName:"Edm.String",structuralProperty:1}},operationType:0,operationName:"cat_RunCopilotTests"})};return Xrm.WebApi.online.execute(c).then((e=>{if(!e.ok)throw new Error("Failed to execute the action.");{const{PROGRESS:e,WARNING:t}=i;this.formContext.ui.setFormNotification(e.message,e.type,e.id),this.removeNotification(e.id),r&&(this.formContext.ui.setFormNotification(r,t.type,t.id),this.removeNotification(t.id))}}))}static onSave(e){if(!e)return;const t=e.getFormContext();if(1===t.ui.getFormType())try{const e=Xrm.Utility.getGlobalContext().getClientUrl(),o=t.getAttribute("cat_copilotconfigurationid").getValue()[0].id.replace(/[{},]/g,""),i=Xrm.WebApi.retrieveRecord("cat_copilotconfiguration",o,"?$select=cat_clientid,cat_tenantid,cat_userauthenticationcode,cat_scope").then((i=>{const r=new d(i.cat_clientid,i.cat_tenantid,i.cat_scope,e,t);let n={authCode:null,codeVerifier:null},a="";if(2===i.cat_userauthenticationcode){if(n=r.authService.getAuthorizationCode(),!n.authCode||!n.codeVerifier)throw new Error("Failed to obtain authorization code or code verifier");a="This agent configuration is configured with end-user authentication, which relies on Entra ID tokens with a limited lifetime. Consider splitting your test set if it takes longer than an hour to complete."}return r.invokeDataverseAction(o,n.authCode,n.codeVerifier,a)}))}catch(e){const{ERROR:o}=i;t.ui.setFormNotification(`An error occurred while running the test. ${e instanceof Error?e.message:"Unknown error"}`,o.type,o.id),d.prototype.removeNotification(o.id)}}}window.onSave=d.onSave;var o=cat="undefined"==typeof cat?{}:cat;for(var i in t)o[i]=t[i];t.__esModule&&Object.defineProperty(o,"__esModule",{value:!0})})();'
                };
            }
            return null;
        },
        
        getElementsByTagName: function(tagName) {
            console.log(`document.getElementsByTagName called with: ${tagName}`);
            if (tagName === 'script') {
                return [this.querySelector('script')];
            }
            return [];
        },
        
        body: {
            appendChild: function(element) {
                console.log("Mock body.appendChild called");
            }
        },
        
        head: {
            appendChild: function(element) {
                console.log("Mock head.appendChild called");
            }
        }
    };
    
    // Make document available globally
    var document = window.document;
}

//=============================================================================
// NAMESPACE AND FRAMEWORK INITIALIZATION  
//=============================================================================

/**
 * Initialize Copilot Agents Test (CAT) namespace for TestRunExecutorService
 */
if (typeof cat === 'undefined') {
    console.log("Creating 'cat' namespace for Copilot Agents Test");
    window.cat = {};
    var cat = window.cat;
}

/**
 * Pre-initialize commonly used form sections for UI tests
 * This prevents errors when tests try to access sections that don't exist
 */
function initializeFormSections() {
    const commonSections = [
        { tab: "tab_general", section: "tab_general_section_multiturntestresults" },
        { tab: "tab_general", section: "tab_general_section_enrichedresults" },
        { tab: "tab_general", section: "tab_general_section_multiturntests" },
        { tab: "partnertab", section: null },
        { tab: "tab_consumer", section: null }
    ];
    
    commonSections.forEach(item => {
        const tab = Xrm.Page.ui.tabs.get(item.tab);
        if (item.section) {
            tab.sections.get(item.section);
        }
    });
    
    console.log("Common form sections pre-initialized");
}

// Initialize form sections
initializeFormSections();

//=============================================================================
// UTILITY FUNCTIONS FOR TEST MANAGEMENT
//=============================================================================

/**
 * Reset mock Xrm state to default values
 * Useful for ensuring clean state between test runs
 */
function resetMockXrm() {
    console.log("Resetting mockXrm to default state");
    
    // Reset form type
    Xrm.Page.ui.setFormType(2); // Update form
    
    // Clear all notifications
    Xrm.Page.ui.clearAllNotifications();
    
    // Reset dialog response
    window.showDialogResponse = true;
    
    // Reinitialize default attribute values
    initializeDefaultAttributes();
    
    console.log("mockXrm reset completed");
}

/**
 * Add form context support for compatibility
 */
Xrm.Page.context = {
    getClientUrl: function () {
        return Xrm.Utility.getGlobalContext().getClientUrl();
    },
    getUserId: function () {
        return Xrm.Utility.getGlobalContext().getUserId();
    }
};

/**
 * Add form event registration capabilities
 */
Xrm.Page.data.entity.addOnSave = function (handler) {
    console.log("OnSave handler registered");
    if (!this._saveHandlers) {
        this._saveHandlers = [];
    }
    this._saveHandlers.push(handler);
};

//=============================================================================
// EXPORT FOR MODULE SYSTEMS
//=============================================================================

/**
 * Make mockXrm utilities available for advanced testing scenarios
 */
window.mockXrmUtils = {
    reset: resetMockXrm,
    initializeAttributes: initializeDefaultAttributes,
    initializeSections: initializeFormSections,
    getAttributeStore: () => attributeStore,
    getControlStore: () => controlStore,
    getSectionStore: () => sectionStore,
    getTabStore: () => tabStore
};

// Final initialization message
console.log("mockXrm.js fully initialized - comprehensive D365 mock environment ready for testing");
