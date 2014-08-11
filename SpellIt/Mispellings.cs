using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace SpellIt
{
    public class Mispellings
    {
        public Dictionary<string, int> WordFrequencies { get; private set; }
        private readonly TrigramIndex trigramIndex;
        private readonly Dictionary<string, string> fuzzyDictionary;

        public Dictionary<string, string> GetFuzzyDictionary()
        {
            return fuzzyDictionary;
        }

        public Mispellings(IEnumerable<string> someWords, IEnumerable<string> correctWords)
        {
            trigramIndex = new TrigramIndex(correctWords);
            WordFrequencies = someWords
                                .GroupBy(w => w, (w, ws) => Tuple.Create(w, ws.Count()))
                                .ToDictionary(it => it.Item1, it => it.Item2);
            var unknownWords = WordFrequencies.Keys.Where(w => !trigramIndex.ContainsWord(w));
            var levensteinInfos = RetrieveLevensteinInfos(unknownWords).ToList();
            fuzzyDictionary = GetFuzzyDictionary(levensteinInfos);
        }
        
        public string GetFixedOrNull(string word)
        {
            return trigramIndex.ContainsWord(word) ? word :
                (fuzzyDictionary.ContainsKey(word) ? fuzzyDictionary[word] : null);
        }

        private Dictionary<string, string> GetFuzzyDictionary(IEnumerable<LevensteinInfo> levensteinInfos)
        {
            const int mispellRatio = 27;
            levensteinInfos = levensteinInfos.ToList();
            //var wordFreqs = wordFrequencies;
            
            //File.WriteAllLines("__Variants_to_dictionary_words.txt", levensteinInfos.GroupBy(info => info.GetWord(), (key, infos) => new { word = key, infos }).OrderByDescending(it => it.infos.Count()).Select(it => it.word + "\t" + String.Join(", ", it.infos.OrderByDescending(i => wordFrequencies.ContainsKey(i.GetDictionaryWord()) ? wordFrequencies[i.GetDictionaryWord()] : 0).Select(i => i.GetDictionaryWord() + " (" + (wordFrequencies.ContainsKey(i.GetDictionaryWord()) ? wordFrequencies[i.GetDictionaryWord()] : 0) + ")"))));

            for (int k = 0; k < 5; k++)
            {
                Console.WriteLine(k + ": " + (double)levensteinInfos.Count(i => i.GetDistance() == k) / levensteinInfos.Count());
            }

            var levensteinGroups = levensteinInfos
                .Where(info =>
                    // I think most frequent words is correct.
                    // Clean levensteinInfos from those which contain most frequent words except the word itself.
                       info.GetDictionaryWord() == info.GetWord() ||
                       WordFrequencies.ContainsKey(info.GetWord()) &&
                       WordFrequencies.ContainsKey(info.GetDictionaryWord()) &&
                       WordFrequencies[info.GetWord()] * mispellRatio <= WordFrequencies[info.GetDictionaryWord()])
                .GroupBy(info => info.GetWord(),
                         (key, infos) => new
                         {
                             word = key,
                             infos =
                         infos.OrderBy(
                             info =>
                             WordFrequencies.ContainsKey(info.GetDictionaryWord())
                                 ? WordFrequencies[info.GetDictionaryWord()]
                                 : 0)
                         });

            var misspellingsIndex = GetMisspellingsIndex(levensteinInfos).ToDictionary(kv => kv.Key, kv => kv.Value);

            return levensteinGroups.Select(
                g =>
                g.infos.ElementAtMax(
                    info =>
                    {
                        var m = info.GetMisspelling();
                        return misspellingsIndex.ContainsKey(m) ? misspellingsIndex[m] : 0;
                    })).ToDictionary
                (info => info.GetWord(), info => info.GetDictionaryWord());
        }

        private IEnumerable<LevensteinInfo> RetrieveLevensteinInfos(IEnumerable<string> words)
        {
            var results = words.SelectMany(w =>
            {
                var editDistance = 2;
                if (w.Length < 10) editDistance = 1;
                if (w.Length < 5) editDistance = 0;
                return FindClosestWords(w, editDistance);
            });
            return results;
        }

        private IEnumerable<LevensteinInfo> FindClosestWords(string word, int editDistance)
        {
            var wordTrigrams = TrigramIndex.GetTrigramsFrom(word);
            return trigramIndex.GetWordListUnion(wordTrigrams).Select(
                dictionaryWord => new LevensteinInfo(dictionaryWord, word)).Where(info => info.GetDistance() <= editDistance);
        }

        private IEnumerable<KeyValuePair<Tuple<string, string>, int>> GetMisspellingsIndex(IEnumerable<LevensteinInfo> levensteinInfos)
        {
            var missIndex = new Dictionary<Tuple<string, string>, int>();
            foreach (var info in levensteinInfos.Where(info => info.GetDistance() == 1))
            {
                AddMisspellingsTo(missIndex, info);
            }
            return missIndex.OrderByDescending(kv => kv.Value).ToArray();
        }

        private static void AddMisspellingsTo(IDictionary<Tuple<string, string>, int> missIndex, LevensteinInfo info)
        {
            var misspelling = info.GetMisspelling();
            if (!missIndex.ContainsKey(misspelling))
                missIndex[misspelling] = 0;
            missIndex[misspelling]++;
        }
    }

    [TestFixture]
    internal class MispellingsTest
    {
        private Mispellings mispellings;

        [SetUp]
        public void Init()
        {
            const string fileName = "qst_25.csvcqa_medical.DataInput.Stemmers.MyStemmer.MyStemmer.csv";
            var words = File.ReadAllLines(fileName).SelectMany(line => line.Split(';')[10].Split(' '));
            mispellings = new Mispellings(words, new[] {"насморк", "ринит", "синусит", "кашель"});
        }

        [Test, Explicit("Dictionary words to mispelled versions")]
        public void SaveCorrectToMispelledVersions()
        {
            var words = mispellings
                .GetFuzzyDictionary()
                .GroupBy(kv => kv.Value, (val, kvs) => Tuple.Create(val, kvs.Select(kv => kv.Key).ToList()))
                .OrderByDescending(it => it.Item2.Count);
            File.WriteAllLines("TestCorrectToMisspelled.txt", words.Select(it => it.Item1 + "\t" + String.Join(" ", it.Item2)));
        }
    }
}
