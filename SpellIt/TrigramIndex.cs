using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace SpellIt
{
	internal class TrigramIndex
	{
	    private List<string> Words { get; set; }
		public Dictionary<string, HashSet<int>> Trigrams { get; private set; }

	    public TrigramIndex(IEnumerable<string> words)
	    {
	        Words = words.ToList();
			Trigrams = CalculateTrigramIndex(Words);
		}

		public List<string> GetWords()
		{
			return Words;
		}

		public bool ContainsWord(string word)
		{
			return Words.Contains(word);
		}

        private Dictionary<string, HashSet<int>> CalculateTrigramIndex(IList<string> words)
		{
			var kgrams = new Dictionary<string, HashSet<int>>();
			for (var i = 0; i < words.Count; i++)
			{
				var word = words[i];
				var wordTrigrams = GetTrigramsFrom(word);
				foreach (var trigram in wordTrigrams)
				{
					if (!kgrams.ContainsKey(trigram))
						kgrams.Add(trigram, new HashSet<int>());
					kgrams[trigram].Add(i);
				}
			}
			return kgrams;
		}

		public static HashSet<string> GetKgramsFrom(string word, int k)
		{
			var kgrams = new HashSet<string>();
			if (k > word.Length)
			{
				return kgrams;
			}
			for (var i = 0; i < (word.Length - k + 1); i++)
			{
				kgrams.Add(word.Substring(i, k));
			}
			kgrams.Add("$" + word.Substring(0, k - 1));
			return kgrams;
		}
		public static HashSet<string> GetTrigramsFrom(string word)
		{
			return GetKgramsFrom(word, 3);
		}

		public HashSet<string> GetWordListUnion(IEnumerable<string> wordTrigrams)
		{
			return new HashSet<string>(wordTrigrams.Where(Trigrams.ContainsKey).SelectMany(t => Trigrams[t]).Distinct().Select(id => Words[id]));
		}
	}

    [TestFixture]
    internal class TrigramIndexTest
	{
		[Test, Explicit]
		public void TestIndexCreation()
		{
		    var index = new TrigramIndex(new List<string>());
			Console.WriteLine(String.Join("\n",
			                              index.Trigrams.OrderByDescending(t => t.Value.Count).Select(
			                              	t => t.Key + "\t" + t.Value.Count)));
		}
	}
}
