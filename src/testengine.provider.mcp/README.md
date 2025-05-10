# Test Engine MCP NodeJS Project

## Overview

The Test Engine MCP Server make use of NodeJS as a **proxy** that bridges the gap between the **Power Apps Test Engine** and **Visual Studio Code**. It implements a **Model Context Protocol (MCP)** server over **STDIO** and connects to the Test Engine to enable the creation and validation of **Power Fx expressions** and **test cases**.

This project is designed to streamline the development and testing of Power Fx expressions by providing an interactive environment within Visual Studio Code, while leveraging the capabilities of the Power Apps Test Engine.

---

## How It Works

1. **MCP Server**:
   - The project implements an MCP server using the `@modelcontextprotocol/sdk` library.
   - The MCP server communicates over **STDIO**, which allows it to integrate seamlessly with Visual Studio Code's MCP client.

2. **Proxy to Power Apps Test Engine**:
   - The NodeJS project acts as a proxy between Visual Studio Code and the Power Apps Test Engine.
   - It forwards requests from Visual Studio Code (e.g., validating Power Fx expressions) to the Test Engine via HTTP.

3. **Power Fx Validation**:
   - The project exposes a `validate-power-fx` tool that allows users to validate Power Fx expressions.
   - The validation requests are sent to the Test Engine, which evaluates the expressions and returns the results.

4. **Test Case Authoring**:
   - Developers can use the MCP server to create and manage test cases for Power Fx expressions.
   - The server interacts with the Test Engine to execute and validate these test cases.

---

## Integration with Visual Studio Code

### 1. **MCP Server Registration**
To enable the MCP server in Visual Studio Code, you need to configure the `settings.json` file. Add the following configuration:

```json
{
    "mcp": {
        "inputs": [],
        "servers": {
            "TestEngine": {
                "command": "node",
                "args": [
                    "./src/testengine.mcp/app.js",
                    "8080"
                ]
            }
        }
    },
    "chat.mcp.discovery.enabled": true
}