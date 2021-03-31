using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace InfoSearch
{
    public class CountTfIdfService : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            const int documentsCount = 100;
            var file = await File.ReadAllLinesAsync(FilePathConstants.InvertedIndexFilePath, cancellationToken);
            var wordsCount = file.Length;

            var list = new List<TfIdfModel>();
            foreach (var line in file)
            {
                var split = line.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList();
                var word = split.First();
                split.Remove(word);

                var model = new TfIdfModel(
                    word,
                    split.Count * 1.0 / wordsCount,
                    Math.Log2(documentsCount * 1.0 / split.Count));
                list.Add(model);
            }

            await using var writer = new StreamWriter(FilePathConstants.TfIdfFilePath);
            foreach (var (word, tf, idf) in list)
            {
                await writer.WriteLineAsync($"{word} {idf} {tf * idf}");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }

        public record TfIdfModel(string Word, double Tf, double Idf);
    }
}