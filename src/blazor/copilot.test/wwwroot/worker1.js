
self.onmessage = async function(event) {
    const { accessToken } = event.data;
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
    let conversationId = "";
    let messages = [];

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

    // Initialize the TestEngine
    await exports.copilot.test.TestEngine.Init(JSON.stringify({ token: accessToken }), "Experimental.Connect()");

    // Function to update conversationId and messages
    async function updateConversation() {
        conversationId = await exports.copilot.test.TestEngine.ConversationId();
        messages = await exports.copilot.test.TestEngine.GetMessages();
        self.postMessage({ conversationId, messages: JSON.stringify(messages) });
    }

    // Update every 1000 seconds
    setInterval(updateConversation, 1000 * 1000);
};


