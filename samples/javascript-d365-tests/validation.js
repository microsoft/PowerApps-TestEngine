/**
 * validation.js - Field validation functions for Dynamics 365
 * These functions would be used to validate form data.
 */

/**
 * Validates a phone number format
 * @param {string} phoneNumber - The phone number to validate
 * @returns {boolean} - True if valid, false otherwise
 */
function validatePhoneNumber(phoneNumber) {
    if (!phoneNumber) return true; // Empty is valid
    
    // Simple validation - must be at least 10 digits
    var digitsOnly = phoneNumber.replace(/\D/g, '');
    return digitsOnly.length >= 10;
}

/**
 * Validates an email address format
 * @param {string} email - The email to validate
 * @returns {boolean} - True if valid, false otherwise
 */
function validateEmail(email) {
    if (!email) return true; // Empty is valid
    
    // Simple validation - must have @ and .
    return email.includes('@') && email.includes('.') && email.indexOf('@') < email.lastIndexOf('.');
}

/**
 * Validates credit limit based on credit score
 * @param {number} creditLimit - The credit limit amount
 * @param {number} creditScore - The credit score
 * @returns {boolean} - True if valid, false otherwise
 */
function validateCreditLimit(creditLimit, creditScore) {
    if (!creditLimit) return true; // Empty is valid
    
    // Based on credit score, enforce maximum credit limit
    if (creditScore < 500) {
        return creditLimit <= 1000;
    } else if (creditScore < 600) {
        return creditLimit <= 5000;
    } else if (creditScore < 700) {
        return creditLimit <= 25000;
    } else {
        // Good credit - no specific validation
        return true;
    }
}

/**
 * Performs validation for all fields on the account form
 * @returns {boolean} - True if all validations pass, false otherwise
 */
function validateAccountForm() {
    var isValid = true;
    
    // Validate phone
    var phone = Xrm.Page.getAttribute("telephone1").getValue();
    var phoneControl = Xrm.Page.getControl("telephone1");
    
    if (!validatePhoneNumber(phone)) {
        phoneControl.setNotification("Phone number must have at least 10 digits", "phone_validation");
        isValid = false;
    } else {
        phoneControl.clearNotification("phone_validation");
    }
    
    // Validate email
    var email = Xrm.Page.getAttribute("emailaddress1").getValue();
    var emailControl = Xrm.Page.getControl("emailaddress1");
    
    if (!validateEmail(email)) {
        emailControl.setNotification("Please enter a valid email address", "email_validation");
        isValid = false;
    } else {
        emailControl.clearNotification("email_validation");
    }
    
    // Validate credit limit
    var creditLimit = Xrm.Page.getAttribute("creditlimit").getValue();
    var creditScore = Xrm.Page.getAttribute("creditscore").getValue() || 0;
    var creditLimitControl = Xrm.Page.getControl("creditlimit");
    
    if (!validateCreditLimit(creditLimit, creditScore)) {
        creditLimitControl.setNotification("Credit limit too high for current credit score", "creditlimit_validation");
        isValid = false;    } else {
        creditLimitControl.clearNotification("creditlimit_validation");
    }
    
    return isValid;
}
