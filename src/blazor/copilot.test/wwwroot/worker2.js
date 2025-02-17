self.onmessage = async function (event) {
    const { config, conversationId, messages, expression } = event.data;
    self.document = {
        createElement: function (tagName) {
            class MockElement {
                constructor(tagName) {
                    this.tagName = tagName;
                    this.children = [];
                    this.attributes = {};
                    this.style = {};
                    this.childNodes = this.children;
                }

                setAttribute(name, value) {
                    this.attributes[name] = value;
                }

                appendChild(child) {
                    this.children.append(child);
                }

                toString() {
                    return `<MockElement tagName=${this.tagName}>`;
                }
            }

            return new MockElement(tagName);
        },
        createElementNS(namespaceURI, qualifiedName) {
            return self.document.createElement(qualifiedName);
        }
    }
    self.window = {
        document: self.document,
        navigator: self.navigator,
        location: self.location,
        localStorage: self.localStorage,
        sessionStorage: self.sessionStorage,
        fetch: self.fetch,
        setTimeout: self.setTimeout,
        clearTimeout: self.clearTimeout,
        console: self.console
    };

    // Include Blazor script
    importScripts('_framework/blazor.webassembly.js');

    // Initialize Blazor
    await self.window.Blazor.start({
        loadBootResource: function (type, name, defaultUri, integrity) {
            console.log(defaultUri);
        }
    });

    // Get exports
    var r = self.window.Blazor.runtime;
    const exports = await r.getAssemblyExports(r.config.mainAssemblyName);

    // Function to execute PowerFx statement
    async function executePowerFx(expression) {
        const result = await exports.copilot.test.TestEngine.ExecuteAsync(JSON.stringify(config), expression);
        self.postMessage(result);
    }

    // Listen for messages from worker1
    self.onmessage = function (event) {
        const { conversationId, messages } = event.data;
        exports.copilot.test.TestEngine.SetMessages(messages);
        console.log("Received from worker1:", conversationId, messages);
    };

    // Execute Power Fx lines of text
    if (expression) {
       executePowerFx(expression);
    }
};