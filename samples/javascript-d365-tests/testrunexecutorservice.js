/**
 * TestRunExecutorService - Expanded and organized version
 * Handles OAuth2/PKCE authentication and Dataverse action execution for Copilot Tests
 */

// Constants and Configuration
const AUTH_CONSTANTS = {
    STATE_KEY: "agent_auth_state",
    POSSIBLE_CHARS: "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789",
    STATE_LENGTH: 32,
    CODE_VERIFIER_LENGTH: 64,
    AUTH_WINDOW_WIDTH: 600,
    AUTH_WINDOW_HEIGHT: 600,
    POLL_INTERVAL: 100,
    AUTH_TIMEOUT: 300000, // 5 minutes
    WIDTH: 600,
    HEIGHT: 600
};

const NOTIFICATION_CONSTANTS = {
    PROGRESS: {
        id: "TESTRUN_ACTION_NOTIFICATION",
        message: "Test Run execution is in progress.",
        type: "INFO"
    },
    WARNING: {
        id: "TESTRUN_WARNING_NOTIFICATION",
        type: "WARNING"
    },
    ERROR: {
        id: "TESTRUN_ONSAVE_NOTIFICATION",
        type: "ERROR"
    }
};

const TIMING_CONSTANTS = {
    WAIT_TIMEOUT: 7000,
    POLL_INTERVAL: 200,
    NOTIFICATION_DELAY: 20000,
    WARNING_DELAY: 5000,
    REMOVAL_DELAY: 12000
};

/**
 * Authentication Service Class
 * Handles OAuth2/PKCE authentication flow with Entra ID
 */
class AuthenticationService {
    constructor(clientId, tenantId, scope, clientUrl) {
        this.clientId = clientId;
        this.tenantId = tenantId;
        this.scopes = [scope];
        this.clientUrl = clientUrl;
        this.baseUrl = `https://login.microsoftonline.com/${tenantId}`;
        this.authUrl = `${this.baseUrl}/oauth2/v2.0/authorize`;
        this.tokenUrl = `${this.baseUrl}/oauth2/v2.0/token`;
    }

    /**
     * Generates a cryptographically secure random string
     * @param {number} length - Length of the string to generate
     * @returns {string} Random string
     */
    generateRandomString(length) {
        return Array.from(crypto.getRandomValues(new Uint8Array(length)))
            .map(byte => AUTH_CONSTANTS.POSSIBLE_CHARS[byte % AUTH_CONSTANTS.POSSIBLE_CHARS.length])
            .join("");
    }

    /**
     * Generates PKCE code verifier
     * @returns {string} Base64URL encoded code verifier
     */
    generateCodeVerifier() {
        const array = new Uint8Array(AUTH_CONSTANTS.CODE_VERIFIER_LENGTH);
        crypto.getRandomValues(array);
        let result = "";
        for (let i = 0; i < array.length; i++) {
            result += AUTH_CONSTANTS.POSSIBLE_CHARS[array[i] % AUTH_CONSTANTS.POSSIBLE_CHARS.length];
        }
        return result;
    }

    /**
     * Generates PKCE code challenge from verifier
     * @param {string} codeVerifier - The code verifier
     * @returns {Promise<string>} Base64URL encoded SHA256 hash
     */
    async generateCodeChallenge(codeVerifier) {
        const encoder = new TextEncoder();
        const data = encoder.encode(codeVerifier);
        const digest = await crypto.subtle.digest("SHA-256", data);
        const hashArray = new Uint8Array(digest);
        return btoa(String.fromCharCode.apply(null, hashArray))
            .replace(/\+/g, "-")
            .replace(/\//g, "_")
            .replace(/=/g, "");
    }

    /**
     * Generates a secure state parameter for CSRF protection
     * @returns {string} Random state string
     */
    generateState() {
        const array = new Uint8Array(AUTH_CONSTANTS.STATE_LENGTH);
        crypto.getRandomValues(array);
        let result = "";
        for (let i = 0; i < array.length; i++) {
            result += AUTH_CONSTANTS.POSSIBLE_CHARS[array[i] % AUTH_CONSTANTS.POSSIBLE_CHARS.length];
        }
        return result;
    }

    /**
     * Initializes the OAuth2 authorization flow
     * @returns {Promise<string>} Authorization URL
     */
    async initializeAuthFlow() {
        const codeVerifier = this.generateCodeVerifier();
        const codeChallenge = await this.generateCodeChallenge(codeVerifier);
        const state = this.generateState();

        // Store PKCE parameters in session storage
        sessionStorage.setItem("code_verifier", codeVerifier);
        sessionStorage.setItem(AUTH_CONSTANTS.STATE_KEY, state);

        const params = new URLSearchParams({
            client_id: this.clientId,
            response_type: "code",
            redirect_uri: this.clientUrl,
            scope: this.scopes.join(" "),
            state: state,
            code_challenge: codeChallenge,
            code_challenge_method: "S256",
            prompt: "select_account"
        });

        return `${this.authUrl}?${params.toString()}`;
    }

    /**
     * Opens authentication popup window
     * @param {string} authUrl - The authorization URL
     * @returns {Window} Popup window reference
     */
    openPopup(authUrl) {
        const left = (screen.width - AUTH_CONSTANTS.WIDTH) / 2;
        const top = (screen.height - AUTH_CONSTANTS.HEIGHT) / 2;
        const features = `width=${AUTH_CONSTANTS.WIDTH},height=${AUTH_CONSTANTS.HEIGHT},left=${left},top=${top},resizable=yes,scrollbars=yes`;
        
        return window.open(authUrl, "authPopup", features);
    }

    /**
     * Polls the popup window for authorization code
     * @param {Window} popup - The popup window reference
     * @returns {Promise<string>} Authorization code
     */
    pollForAuthCode(popup) {
        return new Promise((resolve, reject) => {
            const pollInterval = setInterval(() => {
                if (popup.closed) {
                    clearInterval(pollInterval);
                    reject(new Error("Authentication cancelled by user"));
                    return;
                }

                try {
                    const currentUrl = popup.location.href;
                    if (currentUrl && currentUrl.includes(this.clientUrl)) {
                        clearInterval(pollInterval);
                        popup.close();

                        const urlParams = new URL(currentUrl).searchParams;
                        const code = urlParams.get("code");
                        const state = urlParams.get("state");
                        const error = urlParams.get("error");

                        if (error) {
                            reject(new Error(`Authentication error: ${error}`));
                            return;
                        }

                        const storedState = sessionStorage.getItem(AUTH_CONSTANTS.STATE_KEY);
                        if (state !== storedState) {
                            sessionStorage.removeItem(AUTH_CONSTANTS.STATE_KEY);
                            reject(new Error("State mismatch. Possible CSRF attack."));
                            return;
                        }

                        if (!code) {
                            reject(new Error("No authorization code received"));
                            return;
                        }

                        sessionStorage.removeItem(AUTH_CONSTANTS.STATE_KEY);
                        resolve(code);
                    }
                } catch (error) {
                    // Cross-origin errors are expected during polling
                }

                setTimeout(pollInterval, AUTH_CONSTANTS.POLL_INTERVAL);
            }, AUTH_CONSTANTS.POLL_INTERVAL);

            // Set authentication timeout
            setTimeout(() => {
                clearInterval(pollInterval);
                popup.close();
                reject(new Error("Authentication timeout"));
            }, AUTH_CONSTANTS.AUTH_TIMEOUT);
        });
    }

    /**
     * Exchanges authorization code for access tokens
     * @param {string} authCode - The authorization code
     * @returns {Promise<Object>} Token response
     */
    async exchangeCodeForTokens(authCode) {
        const codeVerifier = sessionStorage.getItem("code_verifier");
        if (!codeVerifier) {
            throw new Error("Code verifier not found");
        }

        sessionStorage.removeItem("code_verifier");

        const tokenParams = new URLSearchParams({
            client_id: this.clientId,
            grant_type: "authorization_code",
            code: authCode,
            redirect_uri: this.clientUrl,
            code_verifier: codeVerifier
        });

        const response = await fetch(this.tokenUrl, {
            method: "POST",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded"
            },
            body: tokenParams.toString()
        });

        if (!response.ok) {
            throw new Error(`Token exchange failed: ${response.statusText}`);
        }

        return await response.json();
    }

    /**
     * Performs complete authentication flow
     * @returns {Promise<Object>} Authentication result
     */
    async authenticate() {
        try {
            const authUrl = await this.initializeAuthFlow();
            const popup = this.openPopup(authUrl);
            
            if (!popup) {
                throw new Error("Failed to open authentication popup");
            }

            const authCode = await this.pollForAuthCode(popup);
            return await this.exchangeCodeForTokens(authCode);
        } catch (error) {
            console.error("Authentication failed:", error);
            throw error;
        }
    }

    /**
     * Gets metadata for Dataverse actions
     * @returns {Object} Metadata object
     */
    getMetadata() {
        return {
            boundParameter: "cat_copilottest"
        };
    }

    /**
     * Executes a Dataverse action with authentication
     * @param {string} actionName - Name of the action
     * @param {Object} parameters - Action parameters
     * @returns {Promise<Object>} Action result
     */
    async executeAction(actionName, parameters) {
        try {
            const tokens = await this.authenticate();
            return await Xrm.WebApi.online.execute({
                name: actionName,
                parameters: parameters
            });
        } catch (error) {
            console.error("Action execution failed:", error);
            throw error;
        }
    }
}

/**
 * Notification Service Class
 * Manages form notifications with auto-removal
 */
class NotificationService {
    constructor(formContext) {
        this.formContext = formContext;
    }

    /**
     * Shows a notification with auto-removal
     * @param {string} message - Notification message
     * @param {string} level - Notification level (INFO, WARNING, ERROR)
     * @param {string} uniqueId - Unique identifier for the notification
     * @returns {string} Notification ID
     */
    showNotification(message, level = "INFO", uniqueId = TIMING_CONSTANTS.WAIT_TIMEOUT) {
        const notification = {
            message: message,
            notificationLevel: level,
            uniqueId: uniqueId
        };

        this.formContext.ui.setFormNotification(
            notification.message,
            notification.notificationLevel,
            notification.uniqueId
        );

        // Auto-remove after specified delay
        setTimeout(() => {
            this.formContext.ui.clearFormNotification(notification.uniqueId);
        }, TIMING_CONSTANTS.NOTIFICATION_DELAY);

        return notification.uniqueId;
    }

    /**
     * Removes a notification with delay
     * @param {string} notificationId - ID of notification to remove
     */
    removeNotification(notificationId) {
        setTimeout(() => {
            this.formContext.ui.clearFormNotification(notificationId);
        }, TIMING_CONSTANTS.REMOVAL_DELAY);
    }

    /**
     * Shows progress notification
     * @returns {string} Notification ID
     */
    showProgressNotification() {
        const notification = NOTIFICATION_CONSTANTS.PROGRESS;
        this.formContext.ui.setFormNotification(
            notification.message,
            notification.type,
            notification.id
        );
        return notification.id;
    }

    /**
     * Shows warning notification with auto-removal
     * @param {string} message - Warning message
     * @returns {string} Notification ID
     */
    showWarningNotification(message) {
        const notification = {
            ...NOTIFICATION_CONSTANTS.WARNING,
            message: message
        };

        this.formContext.ui.setFormNotification(
            notification.message,
            notification.type,
            notification.id
        );

        setTimeout(() => {
            this.formContext.ui.clearFormNotification(notification.id);
        }, TIMING_CONSTANTS.WARNING_DELAY);

        return notification.id;
    }

    /**
     * Shows error notification
     * @param {string} message - Error message
     * @returns {string} Notification ID
     */
    showErrorNotification(message) {
        const notification = {
            ...NOTIFICATION_CONSTANTS.ERROR,
            message: message
        };

        this.formContext.ui.setFormNotification(
            notification.message,
            notification.type,
            notification.id
        );

        return notification.id;
    }
}

/**
 * Test Run Manager Class
 * Coordinates test execution with authentication and notifications
 */
class TestRunManager {
    constructor(config, formContext) {
        this.testRunExecutorService = new AuthenticationService(
            config.clientId,
            config.tenantId,
            config.scope,
            config.clientUrl
        );
        this.notificationService = new NotificationService(formContext);
    }

    /**
     * Executes a test run with full error handling
     * @param {string} testRunId - ID of the test run
     * @returns {Promise<Object>} Execution result
     */
    async executeTestRun(testRunId) {
        const progressId = this.notificationService.showProgressNotification();

        try {
            const result = await this.testRunExecutorService.executeAction("cat_RunCopilotTests", {
                TestRunId: testRunId
            });

            this.notificationService.removeNotification(progressId);
            return result;
        } catch (error) {
            this.notificationService.removeNotification(progressId);
            this.notificationService.showErrorNotification(
                `Test run execution failed: ${error.message}`
            );
            throw error;
        }
    }
}

/**
 * Main TestRunExecutorService Class
 * Entry point for the test execution system
 */
class TestRunExecutorService extends AuthenticationService {
    constructor() {
        super(...arguments);
        this.isTestRunning = false;
    }

    /**
     * Waits for record ID to be available
     * @param {number} timeout - Maximum wait time
     * @param {number} interval - Check interval
     * @returns {Promise<string|null>} Record ID or null
     */
    async waitForRecordId(timeout = TIMING_CONSTANTS.WAIT_TIMEOUT, interval = TIMING_CONSTANTS.POLL_INTERVAL) {
        let recordId = this.formContext.data.entity.getId();
        let attempts = 0;
        const maxAttempts = timeout / interval;

        while (!recordId && attempts < maxAttempts) {
            await new Promise(resolve => setTimeout(resolve, interval));
            recordId = this.formContext.data.entity.getId();
            attempts++;
        }

        return recordId ? recordId.replace(/[{},]/g, "") : null;
    }

    /**
     * Removes notification with delay
     * @param {string} notificationId - Notification ID to remove
     */
    removeNotification(notificationId) {
        setTimeout(() => {
            this.formContext.ui.clearFormNotification(notificationId);
        }, TIMING_CONSTANTS.REMOVAL_DELAY);
    }

    /**
     * Invokes Dataverse action for test execution
     * @param {string} configId - Configuration ID
     * @param {string} authCode - Authorization code
     * @param {string} codeVerifier - PKCE code verifier
     * @param {string} warningMessage - Optional warning message
     * @returns {Promise<Object>} Action result
     */
    async invokeDataverseAction(configId, authCode, codeVerifier, warningMessage) {
        try {
            const testSetId = this.formContext
                .getAttribute("cat_copilottestsetid")
                .getValue()[0].id
                .replace(/[{},]/g, "");

            const entityReference = {
                entityType: this.formContext.data.entity.getEntityName(),
                id: await this.waitForRecordId()
            };

            const actionRequest = {
                entity: entityReference,
                AuthCode: authCode,
                CodeVerifier: codeVerifier,
                CopilotConfigurationId: configId,
                CopilotTestRunId: entityReference.id,
                CopilotTestSetId: testSetId,
                getMetadata: () => ({
                    boundParameter: "entity",
                    parameterTypes: {
                        entity: {
                            typeName: "mscrm.cat_copilottestrun",
                            structuralProperty: 5
                        },
                        AuthCode: {
                            typeName: "Edm.String",
                            structuralProperty: 1
                        },
                        CodeVerifier: {
                            typeName: "Edm.String",
                            structuralProperty: 1
                        },
                        CopilotConfigurationId: {
                            typeName: "Edm.String",
                            structuralProperty: 1
                        },
                        CopilotTestRunId: {
                            typeName: "Edm.String",
                            structuralProperty: 1
                        },
                        CopilotTestSetId: {
                            typeName: "Edm.String",
                            structuralProperty: 1
                        }
                    },
                    operationType: 0,
                    operationName: "cat_RunCopilotTests"
                })
            };

            const response = await Xrm.WebApi.online.execute(actionRequest);

            if (!response.ok) {
                throw new Error("Failed to execute the action.");
            }

            // Show success notification
            const progressNotification = NOTIFICATION_CONSTANTS.PROGRESS;
            this.formContext.ui.setFormNotification(
                progressNotification.message,
                progressNotification.type,
                progressNotification.id
            );
            this.removeNotification(progressNotification.id);

            // Show warning if provided
            if (warningMessage) {
                const warningNotification = NOTIFICATION_CONSTANTS.WARNING;
                this.formContext.ui.setFormNotification(
                    warningMessage,
                    warningNotification.type,
                    warningNotification.id
                );
                this.removeNotification(warningNotification.id);
            }

            return response;
        } catch (error) {
            throw new Error(`Failed to execute the action. Error Message: ${error instanceof Error ? error.message : "Unknown error"}`);
        }
    }

    /**
     * Form save event handler
     * @param {Object} executionContext - Execution context from form
     * @returns {Promise<void>}
     */
    static async onSave(executionContext) {
        if (!executionContext) return;

        const formContext = executionContext.getFormContext();
        
        // Only run on create form
        if (formContext.ui.getFormType() !== 1) return;

        try {
            const clientUrl = Xrm.Utility.getGlobalContext().getClientUrl();
            const configId = formContext
                .getAttribute("cat_copilotconfigurationid")
                .getValue()[0].id
                .replace(/[{},]/g, "");

            // Retrieve configuration details
            const configRecord = await Xrm.WebApi.retrieveRecord(
                "cat_copilotconfiguration",
                configId,
                "?$select=cat_clientid,cat_tenantid,cat_userauthenticationcode,cat_scope"
            );

            const service = new TestRunExecutorService(
                configRecord.cat_clientid,
                configRecord.cat_tenantid,
                configRecord.cat_scope,
                clientUrl,
                formContext
            );

            let authResult = { authCode: null, codeVerifier: null };
            let warningMessage = "";

            // Handle user authentication
            if (configRecord.cat_userauthenticationcode === 2) {
                authResult = await service.authService.getAuthorizationCode();
                
                if (!authResult.authCode || !authResult.codeVerifier) {
                    throw new Error("Failed to obtain authorization code or code verifier");
                }

                warningMessage = "This agent configuration is configured with end-user authentication, which relies on Entra ID tokens with a limited lifetime. Consider splitting your test set if it takes longer than an hour to complete.";
            }

            await service.invokeDataverseAction(
                configId,
                authResult.authCode,
                authResult.codeVerifier,
                warningMessage
            );

        } catch (error) {
            const errorNotification = NOTIFICATION_CONSTANTS.ERROR;
            formContext.ui.setFormNotification(
                `An error occurred while running the test. ${error instanceof Error ? error.message : "Unknown error"}`,
                errorNotification.type,
                errorNotification.id
            );
            TestRunExecutorService.prototype.removeNotification(errorNotification.id);
        }
    }
}

// Global registration
window.onSave = TestRunExecutorService.onSave;

// Module exports for cat namespace
var cat = typeof cat !== "undefined" ? cat : {};
cat.TestRunExecutorService = TestRunExecutorService;
cat.AuthenticationService = AuthenticationService;
cat.NotificationService = NotificationService;
cat.TestRunManager = TestRunManager;

// Export constants for testing
cat.AUTH_CONSTANTS = AUTH_CONSTANTS;
cat.NOTIFICATION_CONSTANTS = NOTIFICATION_CONSTANTS;
cat.TIMING_CONSTANTS = TIMING_CONSTANTS;
