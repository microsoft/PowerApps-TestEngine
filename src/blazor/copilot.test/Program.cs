using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using copilot.test;
using SpawnDev.BlazorJS;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

builder.Services.AddBlazorJSRuntime();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

var host = builder.Build();
WebAssemblyHostInstance.Host = host;

await host.BlazorJSRunAsync();
