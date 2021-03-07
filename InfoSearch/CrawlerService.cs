using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace InfoSearch
{
    public class CrawlerService : IHostedService
    {
        private List<string> Links { get; }
        
        public CrawlerService(IOptions<LinksSource> options)
        {
            Links = options.Value.Links;
        }
        
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var i = 0;
            foreach (var link in Links)
            {
                i++;
                Console.WriteLine($"{i}/{Links.Count}");
                await ProcessPage(link);
            }

            await StopAsync(cancellationToken);
        }

        private async Task ProcessPage(string url)
        {
            var doc = await GetHtmlAsync(url);
            const string folderPath = @"C:\Crawler\";
            var indexFilePath = $"{folderPath}index.txt";
            var uniqueId = Guid.NewGuid();
            var path = $"{folderPath}Pages\\{uniqueId}.txt";

            await using (var sw = new StreamWriter(path, false, System.Text.Encoding.Unicode))
            {
                await sw.WriteLineAsync(ExtractText(doc));
            }

            await using (var w = new StreamWriter(indexFilePath, true, System.Text.Encoding.Unicode))
            {
                await w.WriteLineAsync($"{uniqueId} {url}");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }
        
        private async Task<HtmlDocument> GetHtmlAsync(string url)
        {
            var web = new HtmlWeb();
            var htmlDoc = await web.LoadFromWebAsync(url);
            return htmlDoc;
        }

        private static string Symbols { get; set; } = @"[↑!@#$%^&*()_+=\[{\]};:<>|./?,\\'""-a-zA-z0-9]";

        private static string ExtractText(HtmlDocument document)
        {
            var chunks = document.DocumentNode.DescendantsAndSelf()
                .Where(item => item.NodeType == HtmlNodeType.Text)
                .Where(item => item.Name != "a")
                .Where(item => item.InnerText.Trim() != "")
                .Select(item => item.InnerText.Trim())
                .ToList();

            return string.Join(" ", chunks);
        }
    }
}