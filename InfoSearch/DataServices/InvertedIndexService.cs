using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DeepMorphy;
using InfoSearch.Extensions;
using Microsoft.Extensions.Hosting;

namespace InfoSearch.DataServices
{
    public class InvertedIndexService : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var dictionary = new ConcurrentDictionary<string, ConcurrentBag<string>>();
            var documentDictionary = File.ReadLines(FilePathConstants.IndexFilePath)
                .Select(str => str.Split(' ').First())
                .ToDictionary(
                    index => index,
                    index => File.ReadAllText($"{FilePathConstants.FolderPath}\\Pages\\{index}.txt"));

            var wordsDictionary = File.ReadLines(FilePathConstants.LemmasFilePath)
                .Select(x => x.Split(' '))
                .ToDictionary(x => x.First(), x => x.Distinct());

            var options = new ParallelOptions { MaxDegreeOfParallelism = 10 };
            Parallel.ForEach(documentDictionary, options, document =>
            {
                Parallel.ForEach(wordsDictionary, options, pair =>
                {
                    if (pair.Value.Any(str => Regex.IsMatch(document.Value, str, RegexOptions.IgnoreCase)))
                    {
                        dictionary.AddOrUpdate(pair.Key,
                            key => new ConcurrentBag<string> { document.Key },
                            (key, bag) =>
                            {
                                bag.SafeAdd(document.Key);
                                return bag;
                            });
                    }
                });
            });

            var orderedDictionary = dictionary.Keys
                .OrderBy(key => key)
                .ToDictionary(k => k, k => dictionary[k].OrderBy(r => r).ToList());

            await using var writer = new StreamWriter(FilePathConstants.InvertedIndexFilePath);
            foreach (var (key, value) in orderedDictionary)
            {
                await writer.WriteLineAsync($"{key} {value.Aggregate((source, current) => source + ' ' + current)}");
            }
        }

        public List<Guid> BoolSearch(string searchString)
        {
            var str = searchString.Split(' ');
            var wordToSearch = new List<string>();
            var wordToSkip = new List<string>();

            foreach (var word in str)
            {
                switch (word[0])
                {
                    case '!':
                        wordToSkip.Add(word.Substring(1));
                        break;
                    default:
                        wordToSearch.Add(word);
                        break;
                }
            }

            var morph = new MorphAnalyzer(withLemmatization: true);
            var searchWords = morph.Parse(wordToSearch)
                .Select(r => r.BestTag.Lemma)
                .ToList();
            var skipWords = morph.Parse(wordToSkip)
                .Select(r => r.BestTag.Lemma)
                .ToList();

            var invIndexDict = File.ReadAllLines(FilePathConstants.InvertedIndexFilePath)
                .ToDictionary(key => key.Split(' ').First(),
                    elem => elem.Substring(elem.IndexOf(' ') + 1)
                        .Split(' ')
                        .Select(Guid.Parse)
                        .ToList());

            var skipIndexes = new List<Guid>();
            var foundIndexes = new List<Guid>();

            foreach (var skipWord in skipWords.Where(skipWord => invIndexDict.ContainsKey(skipWord)))
            {
                skipIndexes.AddRange(invIndexDict[skipWord].Where(index => skipIndexes.NotContains(index)));
            }

            foreach (var searchWord in searchWords)
            {
                if (invIndexDict.ContainsKey(searchWord))
                {
                    if (foundIndexes.Count == 0)
                    {
                        foundIndexes.AddRange(invIndexDict[searchWord]);
                    }
                    else
                    {
                        var wordIndexes = invIndexDict[searchWord];
                        foundIndexes = foundIndexes
                            .Where(r => wordIndexes.Contains(r))
                            .ToList();
                    }
                }
                else
                {
                    return new List<Guid>();
                }
            }

            return foundIndexes
                .Except(skipIndexes)
                .ToList();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }
    }
}