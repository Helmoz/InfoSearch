using InfoSearch;
using InfoSearch.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var host = CreateHostBuilder(args).Build();
await host.RunAsync();

static IHostBuilder CreateHostBuilder(string[] args)
{
    return Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((_, builder) =>
        {
            builder.AddJsonFile("links.json");
        })
        .UseStartup<Startup>();
}
    
