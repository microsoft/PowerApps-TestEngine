using System.Collections.Concurrent;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using MediatR;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace testengine.language.server
{ 
    /// <summary>
    /// Handles text document synchronization and completion for a language server.
    /// Implements handlers for text document events such as open, change, close, and save.
    /// </summary>
    public class TextDocumentHandler : ITextDocumentSyncHandler, ICompletionHandler, IDidChangeTextDocumentHandler
    {
        /// <summary>
        /// Options for synchronizing text documents, set to full synchronization.
        /// </summary>
        private readonly TextDocumentSyncRegistrationOptions _syncOptions = new TextDocumentSyncRegistrationOptions
        {
            Change = TextDocumentSyncKind.Full
        };

        /// <summary>
        /// A thread-safe dictionary to store the text of open documents.
        /// </summary>
        private readonly ConcurrentDictionary<string, string> _documentBuffers = new ConcurrentDictionary<string, string>();
        private readonly ILanguageServerFacade _server;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextDocumentHandler"/> class.
        /// </summary>
        /// <param name="server">The language server facade.</param>
        public TextDocumentHandler(ILanguageServerFacade server)
        {
            _server = server;
        }

        /// <summary>
        /// Gets the synchronization options for text documents.
        /// </summary>
        /// <returns>The text document synchronization options.</returns>
        public TextDocumentSyncRegistrationOptions GetRegistrationOptions() => _syncOptions;

        /// <summary>
        /// Handles the event when a text document is opened.
        /// </summary>
        /// <param name="request">The parameters for the open text document event.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
        {
            // Store the text of the opened document in the buffer.
            _documentBuffers[request.TextDocument.Uri.ToString()] = request.TextDocument.Text;
            Console.WriteLine($"Opened: {request.TextDocument.Uri}");
            // Report diagnostics for the opened document.
            ReportDiagnostics(request.TextDocument.Uri.ToString(), request.TextDocument.Text);
            return Unit.Task;
        }

        /// <summary>
        /// Handles the event when a text document is changed.
        /// </summary>
        /// <param name="request">The parameters for the change text document event.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
        {
            if (_documentBuffers.TryGetValue(request.TextDocument.Uri.ToString(), out var buffer))
            {
                // Update the buffer with the new text from the change event.
                foreach (var change in request.ContentChanges)
                {
                    buffer = change.Text; // Assuming full sync, replace the entire buffer
                }
                _documentBuffers[request.TextDocument.Uri.ToString()] = buffer;
                // Report diagnostics for the changed document.
                ReportDiagnostics(request.TextDocument.Uri.ToString(), buffer);
            }
            Console.WriteLine($"Changed: {request.TextDocument.Uri}");
            return Unit.Task;
        }

        /// <summary>
        /// Handles the event when a text document is closed.
        /// </summary>
        /// <param name="request">The parameters for the close text document event.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
        {
            // Remove the closed document from the buffer.
            _documentBuffers.TryRemove(request.TextDocument.Uri.ToString(), out _);
            Console.WriteLine($"Closed: {request.TextDocument.Uri}");
            // Clear diagnostics for the closed document.
            _server.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
            {
                Uri = request.TextDocument.Uri,
                Diagnostics = new Container<Diagnostic>()
            });
            return Unit.Task;
        }

        /// <summary>
        /// Handles completion requests (e.g., when the user requests code completions).
        /// </summary>
        /// <param name="request">The parameters for the completion request.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation, containing the completion list.</returns>
        public Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
        {
            if (_documentBuffers.TryGetValue(request.TextDocument.Uri.ToString(), out var buffer))
            {
                var position = request.Position;
                var offset = GetOffsetFromPosition(buffer, position);

                // Provide completions if the character before the cursor is a dot.
                if (offset > 0 && buffer[offset - 1] == '.')
                {
                    var completions = new[]
                    {
                        new CompletionItem { Label = "Hello", Kind = CompletionItemKind.Text },
                        new CompletionItem { Label = "World", Kind = CompletionItemKind.Text }
                    };

                    return Task.FromResult(new CompletionList(completions));
                }
            }

            return Task.FromResult(new CompletionList());
        }

        /// <summary>
        /// Gets the registration options for completion.
        /// </summary>
        /// <param name="capability">The completion capability.</param>
        /// <param name="clientCapabilities">The client capabilities.</param>
        /// <returns>The completion registration options.</returns>
        public CompletionRegistrationOptions GetRegistrationOptions(CompletionCapability capability, ClientCapabilities clientCapabilities)
        {
            return new CompletionRegistrationOptions
            {
                DocumentSelector = TextDocumentSelector.ForPattern("**/*.txt")
            };
        }

        /// <summary>
        /// Gets the registration options for text document change synchronization.
        /// </summary>
        /// <param name="capability">The text synchronization capability.</param>
        /// <param name="clientCapabilities">The client capabilities.</param>
        /// <returns>The text document change registration options.</returns>
        public TextDocumentChangeRegistrationOptions GetRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)
        {
            return new TextDocumentChangeRegistrationOptions
            {
                DocumentSelector = new TextDocumentSelector(
                    new TextDocumentFilter
                    {
                        Pattern = "**/*.txt"
                    }
                ),
                SyncKind = TextDocumentSyncKind.Full
            };
        }

        /// <summary>
        /// Gets the registration options for text document open events.
        /// </summary>
        /// <param name="capability">The text synchronization capability.</param>
        /// <param name="clientCapabilities">The client capabilities.</param>
        /// <returns>The text document open registration options.</returns>
        TextDocumentOpenRegistrationOptions IRegistration<TextDocumentOpenRegistrationOptions, TextSynchronizationCapability>.GetRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)
        {
            return new TextDocumentOpenRegistrationOptions
            {
                DocumentSelector = new TextDocumentSelector(
                    new TextDocumentFilter
                    {
                        Pattern = "**/*.txt"
                    }
                )
            };
        }

        /// <summary>
        /// Gets the registration options for text document close events.
        /// </summary>
        /// <param name="capability">The text synchronization capability.</param>
        /// <param name="clientCapabilities">The client capabilities.</param>
        /// <returns>The text document close registration options.</returns>
        TextDocumentCloseRegistrationOptions IRegistration<TextDocumentCloseRegistrationOptions, TextSynchronizationCapability>.GetRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)
        {
            return new TextDocumentCloseRegistrationOptions
            {
                DocumentSelector = new TextDocumentSelector(
                    new TextDocumentFilter
                    {
                        Pattern = "**/*.txt"
                    }
                )
            };
        }

        // <summary>
        /// Handles the event when a text document is saved.
        /// </summary>
        /// <param name="request">The parameters for the save text document event.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Saved: {request.TextDocument.Uri}");
            return Unit.Task;
        }

        /// <summary>
        /// Gets the registration options for text document save events.
        /// </summary>
        /// <param name="capability">The text synchronization capability.</param>
        /// <param name="clientCapabilities">The client capabilities.</param>
        /// <returns>The text document save registration options.</returns>
        TextDocumentSaveRegistrationOptions IRegistration<TextDocumentSaveRegistrationOptions, TextSynchronizationCapability>.GetRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)
        {
            return new TextDocumentSaveRegistrationOptions
            {
                DocumentSelector = new TextDocumentSelector(
                    new TextDocumentFilter
                    {
                        Pattern = "**/*.txt"
                    }
                ),
                IncludeText = true
            };
        }

        /// <summary>
        /// Gets the text document attributes for a given document URI.
        /// </summary>
        /// <param name="uri">The document URI</param>
        public TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri)
        {
            return new TextDocumentAttributes(uri, "plaintext");
        }

        /// <summary>
        /// Calculates the offset (character index) from the start of the text to the given position.
        /// </summary>
        /// <param name="text">The text content of the document.</param>
        /// <param name="position">The position (line and character) within the text.</param>
        /// <returns>The offset (character index) corresponding to the given position.</returns>
        private int GetOffsetFromPosition(string text, Position position)
        {
            // Split the text into lines.
            var lines = text.Split('\n');
            var offset = 0;

            // Sum the lengths of all lines before the specified line.
            for (var i = 0; i < position.Line; i++)
            {
                offset += lines[i].Length + 1; // +1 for the newline character.
            }

            // Add the character position within the specified line.
            offset += position.Character;
            return offset;
        }

        /// <summary>
        /// Analyzes the text of a document and reports diagnostics for any uppercase words found.
        /// </summary>
        /// <param name="uri">The URI of the document being analyzed.</param>
        /// <param name="text">The text content of the document.</param>
        private void ReportDiagnostics(string uri, string text)
        {
            // List to hold diagnostic information.
            var diagnostics = new List<Diagnostic>();
            // Split the text into lines.
            var lines = text.Split('\n');

            // Iterate over each line in the text.
            for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                // Split each line into words.
                var words = lines[lineIndex].Split(' ');
                // Iterate over each word in the line.
                for (var wordIndex = 0; wordIndex < words.Length; wordIndex++)
                {
                    var word = words[wordIndex];
                    // Check if the word is entirely uppercase.
                    if (word.All(char.IsUpper))
                    {
                        // Find the start and end positions of the uppercase word.
                        var start = lines[lineIndex].IndexOf(word);
                        var end = start + word.Length;

                        // Add a diagnostic entry for the uppercase word.
                        diagnostics.Add(new Diagnostic
                        {
                            Range = new Range(new Position(lineIndex, start), new Position(lineIndex, end)),
                            Severity = DiagnosticSeverity.Error,
                            Message = $"Uppercase word: {word}",
                            Source = "PlaintextLspServer"
                        });
                    }
                }
            }

            // Publish the diagnostics to the language server.
            _server.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
            {
                Uri = uri,
                Diagnostics = new Container<Diagnostic>(diagnostics)
            });
        }
    }
}
