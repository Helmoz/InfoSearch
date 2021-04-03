using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace InfoSearch.DataServices
{
    public class CountTfIdfService : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var documentIndexesArray = File.ReadLines(FilePathConstants.IndexFilePath)
                .Select(str => str.Split(' ', StringSplitOptions.RemoveEmptyEntries).First())
                .ToArray();

            var documentDictionary = documentIndexesArray
                .ToDictionary(
                    index => index,
                    index => File.ReadAllText($"{FilePathConstants.PagesPath}\\{index}.txt"));

            var invIndexDict = File.ReadAllLines(FilePathConstants.InvertedIndexFilePath)
                .ToDictionary(key => key.Split(' ', StringSplitOptions.RemoveEmptyEntries).First(),
                    elem => elem.Substring(elem.IndexOf(' ') + 1)
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .ToList());

            var lemmasDict = File.ReadAllLines(FilePathConstants.LemmasFilePath)
                .ToDictionary(key => key.Split(' ', StringSplitOptions.RemoveEmptyEntries).First(),
                    elem => elem.Substring(elem.IndexOf(' ') + 1)
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .ToList());

            var wordsByDocumentsDict = GetWordsByDocumentsDict(invIndexDict, lemmasDict, documentDictionary);

            var totalDocumentsCount = (double)documentDictionary.Count;
            var wordsIdfDict = wordsByDocumentsDict
                .ToDictionary(x => x.Key, x => Math.Log(totalDocumentsCount / x.Value.Count));

            var documentsTotalWordsDict = wordsByDocumentsDict
                .SelectMany(x => x.Value)
                .GroupBy(x => x.Key)
                .ToDictionary(x => x.Key, x => x.Sum(y => y.Value));

            var tfIdfByDocumentsDict = GetTfIdfByDocumentsDict(
                wordsByDocumentsDict,
                wordsIdfDict,
                documentsTotalWordsDict);

            WriteToFiles(tfIdfByDocumentsDict);

            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }

        public record TfIdfModel(string Word, double Tf, double Idf);

        private async void WriteToFiles(ConcurrentDictionary<string, ConcurrentDictionary<string, Tuple<double, double>>> tfIdfByDocumentsDict)
        {
            foreach (var (key, dictionary) in tfIdfByDocumentsDict)
            {
                await using var writer = new StreamWriter($"{FilePathConstants.TfIdfFolderPath}\\{key}.txt");

                foreach (var (word, (idf, tfIdf)) in dictionary.OrderBy(x => x.Key))
                {
                    await writer.WriteLineAsync($"{word} {idf} {tfIdf}");
                }
            }
        }

        private ConcurrentDictionary<string, ConcurrentDictionary<string, Tuple<double, double>>> GetTfIdfByDocumentsDict(
            ConcurrentDictionary<string, Dictionary<string, int>> wordsByDocumentsDict,
            Dictionary<string, double> wordsIdfDict,
            Dictionary<string, int> documentsTotalWordsDict)
        {
            var tfIdfByDocumentsDict = new ConcurrentDictionary<string, ConcurrentDictionary<string, Tuple<double, double>>>();

            Parallel.ForEach(wordsByDocumentsDict, new ParallelOptions { MaxDegreeOfParallelism = 10 }, kvp =>
            {
                var (word, wordInDocumentCountDict) = kvp;

                foreach (var (docIndex, wordInDocCount) in wordInDocumentCountDict)
                {
                    var idf = wordsIdfDict[word];
                    var tfIdf = idf * ((double)wordInDocCount / documentsTotalWordsDict[docIndex]);

                    tfIdfByDocumentsDict.AddOrUpdate(
                        docIndex,
                        key =>
                        {
                            var concDict = new ConcurrentDictionary<string, Tuple<double, double>>();
                            concDict.TryAdd(word, new Tuple<double, double>(idf, tfIdf));
                            return concDict;
                        },
                        (key, dict) =>
                        {
                            dict[word] = new Tuple<double, double>(idf, tfIdf);
                            return dict;
                        });
                }
            });

            return tfIdfByDocumentsDict;
        }

        private ConcurrentDictionary<string, Dictionary<string, int>> GetWordsByDocumentsDict(
            Dictionary<string, List<string>> invIndexDict,
            Dictionary<string, List<string>> lemmas,
            Dictionary<string, string> documentDictionary)
        {
            var wordsByDocumentsDict = new ConcurrentDictionary<string, Dictionary<string, int>>();

            Parallel.ForEach(invIndexDict, new ParallelOptions { MaxDegreeOfParallelism = 10 }, kvp =>
            {
                var (word, documentIndexes) = kvp;
                foreach (var documentIndex in documentIndexes)
                {
                    var count = CountWordInCurrentDoc(lemmas[word], documentDictionary[documentIndex]);

                    if (count != 0)
                    {
                        wordsByDocumentsDict.AddOrUpdate(
                            word,
                            key => new Dictionary<string, int>
                            {
                                {
                                    documentIndex, count
                                }
                            },
                            (key, dictionary) =>
                            {
                                dictionary[documentIndex] = count;
                                return dictionary;
                            });
                    }
                }
            });

            return wordsByDocumentsDict;
        }

        private int CountWordInCurrentDoc(IEnumerable<string> lemmas, string htmlDoc) =>
            lemmas.Sum(lemma => Regex.Matches(htmlDoc, @"\b" + lemma + @"\b", RegexOptions.IgnoreCase).Count);
    }
}