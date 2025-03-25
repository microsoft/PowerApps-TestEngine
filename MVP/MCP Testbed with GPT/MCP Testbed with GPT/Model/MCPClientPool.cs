using MCPSharp;
using MCPSharp.Model;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using OpenAITestGenerator;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

class MCPClientPool : ICollection<MCPClient>
{
    private readonly List<MCPClient> clients = [];

    public async Task<List<AITool>> GetAllAIFunctionsAsync()
    {
        var functions = new List<AITool>();
        foreach (var client in clients)
        {
            var clientFunctions = await client.GetFunctionsAsync();
            functions.AddRange(clientFunctions);
        }
        return functions;
    }

    public int Count => clients.Count;
    public bool IsReadOnly => false;
    public void Add(string name, McpServerConfiguration server, Func<Dictionary<string, object>, bool> permissionFunction = null)
    {
        clients.Add(new MCPClient(name, "0.1.0", server.Command, string.Join(' ', server.Args ?? []), server.Env)
        {
            GetPermission = permissionFunction ?? ((parameters) => true)
        });
    }

    public void Add(MCPClient item) => clients.Add(item);
    public void Clear() => clients.Clear();
    public bool Contains(MCPClient item) => clients.Contains(item);
    public void CopyTo(MCPClient[] array, int arrayIndex) => clients.CopyTo(array, arrayIndex);
    public IEnumerator<MCPClient> GetEnumerator() => clients.GetEnumerator();
    public bool Remove(MCPClient item) => clients.Remove(item);
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}