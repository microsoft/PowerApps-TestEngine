import * as vscode from 'vscode';
import { LanguageClient, LanguageClientOptions, ServerOptions } from 'vscode-languageclient/node';

let client: LanguageClient;

/**
 * Activates the language server protocol (LSP) client extension.
 * @param context - The extension context provided by VS Code.
 */
export function activate(context: vscode.ExtensionContext) {
    console.log('Activating lsp')

    // Read the port from the extension settings, defaulting to 8080 if not defined.
    const port = vscode.workspace.getConfiguration('testEngineLspClient').get<number>('serverPort', 8080);

    console.log('Using port ' + port);

    /**
     * Defines the server options for the language client.
     * Creates a connection to the language server running on port 8080.
     * @returns A promise that resolves with the server options.
     */
    const serverOptions: ServerOptions = () => new Promise((resolve, reject) => {
        const socket = require('net').createConnection({ port: port }, () => {
            resolve({
                reader: socket,
                writer: socket
            });
        });
        socket.on('error', (err: any) => reject(err));
    });

    /**
     * Defines the client options for the language client.
     * Specifies the document selector and file synchronization settings.
     */
    const clientOptions: LanguageClientOptions = {
        documentSelector: [{ scheme: 'file', language: 'plaintext' }],
        synchronize: {
            fileEvents: vscode.workspace.createFileSystemWatcher('**/*.txt')
        }
    };

    /**
      * Creates and starts the language client.
      */
    client = new LanguageClient(
        'testEngineLspClient',
        'Test Engine LSP Client',
        serverOptions,
        clientOptions
    );

    // Start the client. This will also launch the server
    client.start();
}

/**
 * Deactivates the language server protocol (LSP) client extension.
 * @returns A promise that resolves when the client has stopped, or undefined if the client is not running.
 */
export function deactivate(): Thenable<void> | undefined {
    if (!client) {
        return undefined;
    }
    return client.stop();
}
