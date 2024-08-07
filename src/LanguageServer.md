# Overview

The Language Service Protocol (LSP) extension for Visual Studio Code enhances the development experience by providing features such as syntax highlighting, code completion, and error checking. This extension leverages the LSP to communicate between the editor and the language server, enabling a seamless and efficient coding environment.

## Key Features
- **Syntax Highlighting**: Automatically highlights syntax based on the language rules, making the code more readable.
- **Code Completion**: Provides intelligent code suggestions to speed up the coding process and reduce errors.
- **Error Checking**: Identifies and highlights errors in real-time, allowing developers to fix issues promptly.

## Getting Started

To get started with running the TypeScript client and .NET server for the Language Service Protocol extension, follow these steps:

1. **Navigate to the Source Directory**
   ```bash
   cd src
   ```

2. **Build the .NET Server**
   ```bash
   dotnet build
   ```

3. Run the server

    ```bash
    cd bin\Debug\testengine.language.server
    dotnet testengine.language.server.dll
    ```

4. **Open the Client Folder in Visual Studio Code**
   - Open Visual Studio Code.
   - Navigate to the `\src\testengine.language.client\src` folder.

5. In Visual Studio Code terminal window install required npm modules

   ```pwsh
   npm install
   ```

6. Compile the extension

   ```pwsh
   npm run compile
   ```

5. **Run the TypeScript Client**
   - Select the `client.ts` file.
   - Press `F5` to start the client using Extension 

By following these steps, you will have the TypeScript client and .NET server up and running, enabling the full functionality of the Language Service Protocol extension in Visual Studio Code.
