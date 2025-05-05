// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

// Generate hash changes with `Get-FileHash -Algorithm SHA256 -Path app.js`

const { McpServer } = require('@modelcontextprotocol/sdk/server/mcp.js');
const { StdioServerTransport } = require('@modelcontextprotocol/sdk/server/stdio.js');
const { z } = require('zod');
const axios = require('axios');

// Get the port number from the command-line arguments
const port = process.argv[2];
if (!port) {
    console.error('Error: Please provide the port number as a command-line argument.');
    process.exit(1);
}

// Function to validate if the port is a valid number
function isValidPort(port) {
    const portNumber = Number(port);
    return Number.isInteger(portNumber) && portNumber > 0 && portNumber <= 65535;
}

if (!port || !isValidPort(port)) {
    console.error('Error: Please provide a valid port number (1-65535) as a command-line argument.');
    process.exit(1);
}

console.log('Port:', port);

// Function to validate Power Fx expressions via HTTP
async function validatePowerFx(powerFx) {
    try {
        // Send a POST request to the .NET server
        const response = await axios.post(`http://localhost:${port}/validate`, powerFx, {
            headers: { 'Content-Type': 'text/plain' },
        });
        console.log('Response from .NET server:', response.data);
        return JSON.stringify(response.data); // Return the JSON response as a string
    } catch (error) {
        console.error('Error communicating with .NET server:', error.message);
        return JSON.stringify({ valid: false, errors: ['Failed to communicate with the .NET server.'] });
    }
}

// Initialize the MCP server
const server = new McpServer({
    name: 'testEngineServer',
    description: 'A server that provides tools for authoring test engine tests',
    version: '1.0.0',
});

// Tool: Validate Power Fx
server.tool(
    "validate-power-fx",
    { powerFx: z.string() },
    async (request) => {
        console.log('Raw request received:', request);
        const powerFx = request.powerFx || '';
        console.log('Received Power Fx for validation:', powerFx);
        if (!powerFx) {
            return {
                content: [{ type: "text", text: JSON.stringify({ valid: false, errors: ['Power Fx string is empty.'] }) }]
            };
        }

        try {
            const validationResult = await validatePowerFx(powerFx);
            return {
                content: [{ type: "text", text: validationResult }]
            };
        } catch (error) {
            console.error('Error validating Power Fx:', error);
            return {
                content: [{ type: "text", text: JSON.stringify({ valid: false, errors: ['An error occurred while validating the Power Fx string.'] }) }]
            };
        }
    }
);

const transport = new StdioServerTransport();
server.connect(transport);

console.log('The Test Engine MCP server is running!');