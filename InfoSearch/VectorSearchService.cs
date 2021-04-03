using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeepMorphy;

namespace InfoSearch
{
    public class VectorSearchService
    {
        private bool _isLoaded;

        private Dictionary<string, List<Guid>> _invertedIndexesDict;

        private string[] _documentIndexesArray;

        private Dictionary<string, string> _actualUrlDictionary;

        private Dictionary<string, Dictionary<string, double>> _documentDictionary;

        public Dictionary<string, double> PerformVectorSearch(string searchString)
        {
            if (!_isLoaded)
            {
                _invertedIndexesDict = File.ReadAllLines(FilePathConstants.InvertedIndexFilePath)
                    .ToDictionary(key => key.Split(' ', StringSplitOptions.RemoveEmptyEntries).First(),
                        elem => elem.Substring(elem.IndexOf(' ') + 1)
                            .Split(' ')
                            .Select(Guid.Parse)
                            .ToList());
                _documentIndexesArray = File.ReadLines(FilePathConstants.IndexFilePath)
                    .Select(str => str.Split(' ', StringSplitOptions.RemoveEmptyEntries).First())
                    .ToArray();
                _actualUrlDictionary = File.ReadLines(FilePathConstants.IndexFilePath)
                    .Select(str => str.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    .ToDictionary(x => x.First(), x => x.Last());
                _documentDictionary = _documentIndexesArray.ToDictionary(
                    x => x,
                    index =>
                        File.ReadLines($"{FilePathConstants.TfIdfFolderPath}\\{index}.txt")
                            .ToDictionary(
                                key => key.Split(' ', StringSplitOptions.RemoveEmptyEntries).First(),
                                value => Math.Round(Convert.ToDouble(value.Split(' ').Last()), 5)));
                _isLoaded = true;
            }

            var morph = new MorphAnalyzer(true);
            var inputWords = morph.Parse(searchString.Split(' ')).Select(r => r.BestTag.Lemma).ToList();
            var wordDictionary = new Dictionary<string, double>();
            foreach (var inputWord in inputWords)
            {
                var equalWordCount = inputWords.Count(word => word == inputWord);
                if (!wordDictionary.ContainsKey(inputWord))
                {
                    wordDictionary.Add(inputWord,
                        CalculateWordVector(inputWord, equalWordCount, inputWords.Count));
                }
            }

            wordDictionary = wordDictionary.Where(item => item.Value != 0)
                .ToDictionary(item => item.Key, item => item.Value);

            var answers = new Dictionary<string, double>();
            foreach (var index in _documentIndexesArray)
            {
                var documentVector = _documentDictionary[index];
                var helpfulFeatures = documentVector.Where(item => wordDictionary.ContainsKey(item.Key))
                    .ToDictionary(item => item.Key, item => item.Value);
                if (helpfulFeatures.Count != 0)
                {
                    answers.Add(index, Math.Round(helpfulFeatures.Sum(item =>
                                                      item.Value * wordDictionary[item.Key]) /
                                                  (Math.Sqrt(wordDictionary
                                                       .Where(item => helpfulFeatures.ContainsKey(item.Key))
                                                       .Sum(item => Math.Pow(item.Value, 2))) *
                                                   Math.Sqrt(helpfulFeatures.Sum(item => Math.Pow(item.Value, 2)))),
                        5));
                }
                else
                {
                    answers.Add(index, 0);
                }
            }

            return answers.OrderByDescending(item => item.Value)
                .Where(x => x.Value != 0)
                .ToDictionary(item => _actualUrlDictionary[item.Key], item => item.Value);
        }

        private double CalculateWordVector(string word, int equalWordCount, int totalWordCount)
        {
            return !_invertedIndexesDict.ContainsKey(word)
                ? 0.0
                : Math.Round(
                    (double)equalWordCount / totalWordCount *
                    Math.Log((double)100 / _invertedIndexesDict[word].Count), 5);
        }
    }
}