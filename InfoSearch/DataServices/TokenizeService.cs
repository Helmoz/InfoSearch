using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InfoSearch.Lib;
using Microsoft.Extensions.Hosting;

namespace InfoSearch.DataServices
{
    public class TokenizeService : IHostedService
    {
        private char[] Delimiters { get; } = {
            '\n', '\t', '\r', ':', ';', '(', ')', '.', ',', ' ', '[', ']', '-', '"', '{', '}', '!', '?',
            '@', '$', '=', '^', '/', '\\', '°', '#', '*', '|', '§', '·', '—', '»', '«'
        };

        private List<string> StopWords { get; }

        public TokenizeService()
        {
            StopWords = StopWord.StopWords.GetStopWords("ru").ToList();
            StopWords.AddRange(new []{ "б", "д", "см", "фр", "ф", "л", "э", "п", "н", "гл", "ю", "др", "стб", "гц", "ин", "сф", "рга"});
        }

        private ConcurrentBag<string> Tokens { get; } = new();

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var txtImporter = new TextImporter();
            var texts = txtImporter.ImportTexts(@"C:\Crawler\Pages").ToList();

            var tasks = texts.Select(ProcessText);

            await Task.WhenAll(tasks);

            await using var w = new StreamWriter(FilePathConstants.TokensFilePath, false, System.Text.Encoding.Unicode);
            foreach (var token in Tokens.Distinct())
            {
                await w.WriteLineAsync(token);
            }

            await StopAsync(cancellationToken);
        }

        private Task ProcessText(string text)
        {
            var cleanText = text.Clean();
            var tokens = cleanText.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries)
                .Select(word => word.Trim().ToLowerInvariant())
                .Where(word => !StopWords.Contains(word))
                .Where(x => x.All(letter => letter >= 'а' && letter <= 'я'))
                .ToList();

            foreach (var token in tokens)
            {
                Tokens.Add(token);
            }

            return Task.CompletedTask;;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }
    }
}