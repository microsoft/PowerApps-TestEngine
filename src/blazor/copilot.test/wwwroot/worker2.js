self.onmessage = async function (event) {
    const { location, baseURI, config, conversationId, messages, expression } = event.data;
    self.document = {
        baseURI: baseURI,
        location: JSON.parse(location),
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
        },
        documentElement: {
            style: {
                setProperty: function (name, value) {
                    console.log(`Property ${name} set to ${value}`);
                }
            }
        },
        childNodes: []
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
        console: self.console,
        addEventListener: function (type, listener, options) {
            console.log(`Event listener added for ${type}`);
        }
    };

    self.history = {
        state: null,
        stateStack: [],
        currentStateIndex: -1,
        pushState: function (state, title, url) {
            this.stateStack = this.stateStack.slice(0, this.currentStateIndex + 1);
            this.stateStack.push(state);
            this.currentStateIndex++;
            console.log(`History pushState: ${url}, state: ${JSON.stringify(state)}`);
        },
        replaceState: function (state, title, url) {
            if (this.currentStateIndex >= 0) {
                this.stateStack[this.currentStateIndex] = state;
                console.log(`History replaceState: ${url}, state: ${JSON.stringify(state)}`);
            }
        },
        back: function () {
            if (this.currentStateIndex > 0) {
                this.currentStateIndex--;
                console.log('History back');
            }
        },
        forward: function () {
            if (this.currentStateIndex < this.stateStack.length - 1) {
                this.currentStateIndex++;
                console.log('History forward');
            }
        },
        go: function (delta) {
            const newIndex = this.currentStateIndex + delta;
            if (newIndex >= 0 && newIndex < this.stateStack.length) {
                this.currentStateIndex = newIndex;
                console.log(`History go: ${delta}`);
            }
        },
        get state() {
            return this.stateStack[this.currentStateIndex] || null;
        }
    };

    if (typeof self.loaded === "undefined") {
        // Include Blazor script
        importScripts('_framework/blazor.webassembly.js');

        // Initialize Blazor
        await self.window.Blazor.start({
            loadBootResource: function (type, name, defaultUri, integrity) {
                console.log(defaultUri);
            }
        });

        self.loaded = true
    }

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