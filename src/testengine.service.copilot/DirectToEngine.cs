using System.Net.Http.Headers;
using Microsoft.Agents.CopilotStudio.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace testengine.service.copilot;




public class DirectToEngine
{
    public void Start() {

        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        // Create an http client for use by the DirectToEngine Client and add the token handler to the client.
        builder.Services.AddHttpClient("mcs")
            .ConfigurePrimaryHttpMessageHandler(() => new AddTokenHandler(settings));

        // add Settings and an instance of the Direct To engine Copilot Client to the Current services.  
        builder.Services
            .AddSingleton(settings)
            .AddTransient<CopilotClient>((s) =>
            {
                var logger = s.GetRequiredService<ILoggerFactory>().CreateLogger<CopilotClient>();
                return new CopilotClient(settings, s.GetRequiredService<IHttpClientFactory>(), logger, "mcs");
            });

        IHost host = builder.Build();
            host.Run();

    }

}
