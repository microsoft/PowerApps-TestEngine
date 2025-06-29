/**
 * formScripts.js - Common form script functions for Dynamics 365
 * These functions would be registered as event handlers for form events.
 */

/**
 * Handles the form onLoad event
 * @param {object} executionContext - The execution context
 * @returns {boolean} - True if operation was successful
 */
function onLoad(executionContext) {
    // Set default values for new forms
    var formType = Xrm.Page.ui.getFormType();
    
    // Form type 1 = Create
    if (formType === 1) {
        var countryAttribute = Xrm.Page.getAttribute("address1_country");
        if (countryAttribute && !countryAttribute.getValue()) {
            countryAttribute.setValue("United States");
        }
    }
    
    // Set up field event handlers
    setupFieldEventHandlers();
    
    return true;
}

/**
 * Set up event handlers for various fields
 * @returns {boolean} - True if operation was successful
 */
function setupFieldEventHandlers() {
    var creditLimitAttribute = Xrm.Page.getAttribute("creditlimit");
    if (creditLimitAttribute) {
        creditLimitAttribute.addOnChange(creditLimitOnChange);
    }
    
    var customerTypeAttribute = Xrm.Page.getAttribute("customertype");
    if (customerTypeAttribute) {
        customerTypeAttribute.addOnChange(customerTypeOnChange);
    }
    
    return true;
}

/**
 * Handles changes to the credit limit field
 * @returns {boolean} - True if operation was successful
 */
function creditLimitOnChange() {
    var creditLimit = Xrm.Page.getAttribute("creditlimit").getValue();
    var creditScoreAttribute = Xrm.Page.getAttribute("creditscore");
    
    if (creditLimit && creditLimit > 10000) {
        // For high credit limits, ensure credit score is recorded
        if (creditScoreAttribute) {
            creditScoreAttribute.setRequiredLevel("required");
        }
    } else {
        // For normal credit limits, credit score is recommended
        if (creditScoreAttribute) {
            creditScoreAttribute.setRequiredLevel("recommended");
        }
    }
    
    return true;
}

/**
 * Handles changes to the customer type field
 * @returns {boolean} - True if operation was successful
 */
function customerTypeOnChange() {
    var customerType = Xrm.Page.getAttribute("customertype").getValue();
    
    // Show/hide tabs based on customer type
    var partnerTab = Xrm.Page.ui.tabs.get("partnertab");
    var consumerTab = Xrm.Page.ui.tabs.get("tab_consumer");
    
    if (customerType === 2) { // 2 = Partner
        if (partnerTab) partnerTab.setVisible(true);
        if (consumerTab) consumerTab.setVisible(false);
    } else if (customerType === 1) { // 1 = Consumer
        if (partnerTab) partnerTab.setVisible(false);
        if (consumerTab) consumerTab.setVisible(true);
    } else {
        // Default case - show both tabs
        if (partnerTab) partnerTab.setVisible(true);
        if (consumerTab) consumerTab.setVisible(true);
    }
    
    return true;
}
