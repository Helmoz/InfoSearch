using InfoSearch.DataServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InfoSearch
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var section = Configuration.GetSection(nameof(LinksSource));
            services.Configure<LinksSource>(section);
            //services.AddHostedService<CrawlerService>();
            //services.AddHostedService<TokenizeService>();
            //services.AddHostedService<LemmatizeService>();
            //services.AddHostedService<InvertedIndexService>();
            services.AddHostedService<CountTfIdfService>();
        }
    }
}