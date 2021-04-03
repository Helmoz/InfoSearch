using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DeepMorphy;
using InfoSearch.Extensions;
using Microsoft.Extensions.Hosting;

namespace InfoSearch.DataServices
{
    public class LemmatizeService : IHostedService
    {
        private ConcurrentDictionary<string, ConcurrentBag<string>> Lemmas { get; set; } = new();

        private MorphAnalyzer MorphAnalyzer { get; set; } = new(true);

        private ParallelOptions ParallelOptions { get; set; } = new() {MaxDegreeOfParallelism = 10};

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var words = File.ReadLines(FilePathConstants.TokensFilePath);
            var parsedWords = MorphAnalyzer.Parse(words).ToList();

            Parallel.ForEach(parsedWords, ParallelOptions, word =>
            {
                Lemmas.AddOrUpdate(
                    word.BestTag.Lemma,
                    _ => new ConcurrentBag<string> { word.Text },
                    (_, bag) =>
                    {
                       bag.SafeAdd(word.Text);
                       return bag;
                    });
            });

            await using var w = new StreamWriter(FilePathConstants.LemmasFilePath);
            foreach (var (key, value) in Lemmas)
            {
                await w.WriteLineAsync($"{key} {string.Join(" ", value)}");
            }

            await StopAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }
    }
}