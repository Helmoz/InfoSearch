using System;
using System.Collections.Concurrent;
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

        private ConcurrentBag<string> Index { get; } = new();

        public CrawlerService(IOptions<LinksSource> options)
        {
            Links = options.Value.Links;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var indexFilePath = $"{FilePathConstants.FolderPath}\\index.txt";

            var tasks = Links.Select(ProcessPage);

            await Task.WhenAll(tasks);

            await using var w = new StreamWriter(indexFilePath, true, System.Text.Encoding.Unicode);
            await w.WriteLineAsync(string.Join(Environment.NewLine, Index));


            await StopAsync(cancellationToken);
        }

        private async Task ProcessPage(string url)
        {
            var doc = await GetHtmlAsync(url);
            var uniqueId = Guid.NewGuid();
            var path = $"{FilePathConstants.FolderPath}\\Pages\\{uniqueId}.txt";

            await using var sw = new StreamWriter(path, false, System.Text.Encoding.Unicode);
            await sw.WriteLineAsync(doc.ParsedText);
            Index.Add($"{uniqueId} {url}");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }

        private static async Task<HtmlDocument> GetHtmlAsync(string url)
        {
            var web = new HtmlWeb();
            var htmlDoc = await web.LoadFromWebAsync(url);
            return htmlDoc;
        }
    }
}