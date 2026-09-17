// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerApps.TestEngine.MCP.Visitor
{
    /// <summary>
    /// Node type enumeration representing the various types of nodes that can be visited in the workspace.
    /// </summary>
    public enum NodeType
    {
        /// <summary>
        /// Represents a directory node.
        /// </summary>
        Directory,

        /// <summary>
        /// Represents a file node.
        /// </summary>
        File,

        /// <summary>
        /// Represents an object node (typically from YAML or JSON content).
        /// </summary>
        Object,

        /// <summary>
        /// Represents a property node within an object.
        /// </summary>
        Property,

        /// <summary>
        /// Represents a function node (typically found within expressions).
        /// </summary>
        Function
    }

    /// <summary>
    /// Base abstract class for all workspace node types.
    /// </summary>
    public abstract class Node
    {
        /// <summary>
        /// Gets or sets the name of the node.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the relative path of the node within the workspace.
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// Gets or sets the full hierarchical path of the node.
        /// </summary>
        public string? FullPath { get; set; }

        /// <summary>
        /// Gets or sets the type of the node.
        /// </summary>
        public NodeType Type { get; set; }
    }

    /// <summary>
    /// Represents a directory in the workspace.
    /// </summary>
    public class DirectoryNode : Node
    {
    }

    /// <summary>
    /// Represents a file in the workspace.
    /// </summary>
    public class FileNode : Node
    {
        /// <summary>
        /// Gets or sets the file extension (without the leading dot).
        /// </summary>
        public string? Extension { get; set; }
    }

    /// <summary>
    /// Represents an object in a file (typically from YAML or JSON content).
    /// </summary>
    public class ObjectNode : Node
    {
    }

    /// <summary>
    /// Represents a property in an object.
    /// </summary>
    public class PropertyNode : Node
    {
        /// <summary>
        /// Gets or sets the string value of the property.
        /// </summary>
        public string? Value { get; set; }
    }

    /// <summary>
    /// Represents a function call found in a property value or expression.
    /// </summary>
    public class FunctionNode : Node
    {
    }
}
