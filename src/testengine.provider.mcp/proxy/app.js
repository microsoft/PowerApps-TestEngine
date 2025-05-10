// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

const { McpServer } = require('@modelcontextprotocol/sdk/server/mcp.js');
const { StdioServerTransport } = require('@modelcontextprotocol/sdk/server/stdio.js');
const { z } = require('zod');
const axios = require('axios');

// Get the port number from the command-line arguments
const port = process.argv[2];
if (!port || !isValidPort(port)) {
    console.error('Error: Please provide a valid port number (1-65535) as a command-line argument.');
    process.exit(1);
}

console.log('Port:', port);

// Function to validate if the port is a valid number
function isValidPort(port) {
    const portNumber = Number(port);
    return Number.isInteger(portNumber) && portNumber > 0 && portNumber <= 65535;
}

// Function to make HTTP requests to the .NET server
async function makeHttpRequest(endpoint, method = 'GET', data = null) {
    try {
        const url = `http://localhost:${port}/${endpoint}`;
        const options = {
            method,
            url,
            headers: { 'Content-Type': 'application/json' },
            data,
        };
        const response = await axios(options);
        return response.data;
    } catch (error) {
        console.error(`Error communicating with .NET server at ${endpoint}:`, error.message);
        return { error: `Failed to communicate with the .NET server at ${endpoint}.` };
    }
}

// Initialize the MCP server
const server = new McpServer({
    name: 'testEngineServer',
    description: 'A server that provides tools for authoring test engine tests and managing plans',
    version: '1.0.0',
});

// Tool: Validate Power Fx
server.tool(
    "validate-power-fx",
    { powerFx: z.string() },
    async (request) => {
        const powerFx = request.powerFx || '';
        if (!powerFx) {
            return { content: [{ type: "text", text: JSON.stringify({ valid: false, errors: ['Power Fx string is empty.'] }) }] };
        }

        const validationResult = await makeHttpRequest('validate', 'POST', powerFx);
        return { content: [{ type: "text", text: JSON.stringify(validationResult) }] };
    }
);

// Tool: Get List of Plan Designer Plans
server.tool(
    "get-plan-list",
    {},
    async () => {
        const plans = await makeHttpRequest('plans');
        return { content: [{ type: "text", text: JSON.stringify(plans) }] };
    }
);

// Tool: Get Details for a Specific Plan
server.tool(
    "get-plan-details",
    { planId: z.string() },
    async (request) => {
        const { planId } = request;
        const planDetails = await makeHttpRequest(`plans/${planId}`);
        return { content: [{ type: "text", text: JSON.stringify(planDetails) }] };
    }
);

// Tool: Get Artifacts for a Plan
server.tool(
    "get-plan-artifacts",
    { planId: z.string() },
    async (request) => {
        const { planId } = request;
        const artifacts = await makeHttpRequest(`plans/${planId}/artifacts`);
        return { content: [{ type: "text", text: JSON.stringify(artifacts) }] };
    }
);

// Tool: Get Solution Assets for a Plan
server.tool(
    "get-solution-assets",
    { planId: z.string() },
    async (request) => {
        const { planId } = request;
        const assets = await makeHttpRequest(`plans/${planId}/assets`);
        return { content: [{ type: "text", text: JSON.stringify(assets) }] };
    }
);

const transport = new StdioServerTransport();
server.connect(transport);

console.log('The Test Engine MCP server is running!');