/**
 * commandBar.js - Common command bar functions for Dynamics 365
 * These functions would be called directly from command bar buttons.
 */

/**
 * Determines if the specified command should be enabled based on the current entity state
 * @param {string} commandName - Name of the command
 * @returns {boolean} - True if the command should be enabled, false otherwise
 */
function isCommandEnabled(commandName) {
    // Get the current entity state
    var accountStatus = Xrm.Page.getAttribute("accountstatus");
    var statusValue = accountStatus ? accountStatus.getValue() : null;
    
    // Determine which commands should be enabled
    switch(commandName) {
        case "new":
            // New command is always available
            return true;
            
        case "activate":
            // Activate command is only available for inactive records
            return statusValue === 0; // 0 = Inactive
            
        case "deactivate":
            // Deactivate command is only available for active records
            return statusValue === 1; // 1 = Active

        default:
            // Unknown command
            return false;
    }
};

/**
 * Validates if an account can be deactivated based on business rules
 * @returns {boolean} - True if deactivation is allowed, false otherwise
 */
CommandBar.validateBeforeDeactivate = function() {
    // Check if there are active cases
    var activeCasesCount = Xrm.Page.getAttribute("activecases_count");
    
    // Check if there are active cases
    if (activeCasesCount && activeCasesCount.getValue() > 0) {
        Xrm.Utility.alertDialog("Cannot deactivate account with active cases.");
        return false;
    }
    
    return true;
};

/**
 * Activates the current account record
 * @returns {boolean} - True if operation was successful
 */
CommandBar.activateAccount = function() {
    // Implementation would call WebAPI to update record
    var accountStatus = Xrm.Page.getAttribute("accountstatus");
    if (accountStatus) {
        accountStatus.setValue(1); // 1 = Active
        return true;
    }
    return false;
};

/**
 * Deactivates the current account record
 * @returns {boolean} - True if operation was successful
 */
CommandBar.deactivateAccount = function() {
    // Check if deactivation is allowed
    if (!CommandBar.validateBeforeDeactivate()) {
        return false;
    }
    
    // Implementation would call WebAPI to update record
    var accountStatus = Xrm.Page.getAttribute("accountstatus");
    if (accountStatus) {
        accountStatus.setValue(0); // 0 = Inactive
        return true;
    }
    return false;
};
