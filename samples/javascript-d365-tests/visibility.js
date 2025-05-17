/**
 * visibility.js - Form visibility functions for Dynamics 365
 * These functions are used to control field, section, and tab visibility.
 */

/**
 * Updates visibility of fields and sections based on account type
 * @returns {boolean} - True if operation was successful
 */
function updateAccountTypeVisibility() {
    var accountType = Xrm.Page.getAttribute("accounttype").getValue();
    
    // Show/hide fields based on account type
    if (accountType === 1) { // 1 = Customer
        // Customer-specific fields
        showHideControl("customerid", true);
        showHideControl("creditlimit", true);
        showHideSection("customer_section", true);
        
        // Hide vendor fields
        showHideControl("vendorid", false);
        showHideSection("vendor_section", false);
    } else if (accountType === 2) { // 2 = Vendor
        // Vendor-specific fields
        showHideControl("vendorid", true);
        showHideControl("vendorrating", true);
        showHideSection("vendor_section", true);
        
        // Hide customer fields
        showHideControl("customerid", false);
        showHideControl("creditlimit", false);
        showHideSection("customer_section", false);
    } else {
        // Default - hide all special sections
        showHideControl("customerid", false);
        showHideControl("vendorid", false);
        showHideSection("customer_section", false);
        showHideSection("vendor_section", false);
    }
    
    return true;
}

/**
 * Updates visibility based on the selected industry
 * @returns {boolean} - True if operation was successful
 */
function updateIndustryBasedVisibility() {
    var industryCode = Xrm.Page.getAttribute("industrycode").getValue();
    
    // Show financial tab for financial industry (code 1)
    if (industryCode === 1) {
        showHideTab("financial", true);
        showHideSection("compliance", true);
    } else {
        showHideTab("financial", false);
        showHideSection("compliance", false);
    }
    
    return true;
}

/**
 * Shows or hides a field control
 * @param {string} controlName - Name of the control to show/hide
 * @param {boolean} visible - True to show, false to hide
 * @returns {boolean} - True if operation was successful
 */
function showHideControl(controlName, visible) {
    var control = Xrm.Page.getControl(controlName);
    if (control) {
        control.setVisible(visible);
        return true;
    }
    return false;
}

/**
 * Shows or hides a section
 * @param {string} sectionName - Name of the section to show/hide
 * @param {boolean} visible - True to show, false to hide
 * @returns {boolean} - True if operation was successful
 */
function showHideSection(sectionName, visible) {
    var section = Xrm.Page.ui.sections.get(sectionName);
    if (section) {
        section.setVisible(visible);
        return true;
    }
    return false;
}

/**
 * Shows or hides a tab
 * @param {string} tabName - Name of the tab to show/hide
 * @param {boolean} visible - True to show, false to hide
 * @returns {boolean} - True if operation was successful
 */
function showHideTab(tabName, visible) {
    var tab = Xrm.Page.ui.tabs.get(tabName);
    if (tab) {
        tab.setVisible(visible);
        return true;
    }
    return false;
}
