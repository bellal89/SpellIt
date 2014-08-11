using System;
using NUnit.Framework;

namespace SpellIt
{
	internal class LevensteinInfo
	{
		private readonly string word;
		private readonly string dictionaryWord;
		private readonly int[,] matrix;

		public LevensteinInfo(string dictionaryWord, string word)
		{
			this.word = word;
			this.dictionaryWord = dictionaryWord;
			matrix = CalculateLevensteinMatrix(word, dictionaryWord);
		}

		public string GetWord()
		{
			return word;
		}

		public string GetDictionaryWord()
		{
			return dictionaryWord;
		}

		public int GetDistance()
		{
			return matrix[word.Length, dictionaryWord.Length];
		}

		private static int[,] CalculateLevensteinMatrix(string s1, string s2)
		{
			if (s1 == null) throw new ArgumentNullException("s1");
			if (s2 == null) throw new ArgumentNullException("s2");
			var m = new int[s1.Length + 1, s2.Length + 1];

			for (int i = 0; i <= s1.Length; i++) m[i, 0] = i;
			for (int j = 0; j <= s2.Length; j++) m[0, j] = j;

			for (int i = 1; i <= s1.Length; i++)
				for (int j = 1; j <= s2.Length; j++)
				{
					int diff = (s1[i - 1] == s2[j - 1]) ? 0 : 1;

					var del = m[i - 1, j] + 1; // deletion
					var ins = m[i, j - 1] + 1; // insertion
					var subst = m[i - 1, j - 1] + diff; // substitution

					m[i, j] = Math.Min(Math.Min(del, ins), subst);

					// transition
					if (i > 1 && j > 1 && s1[i - 1] == s2[j - 2] && s1[i - 2] == s2[j - 1])
					{
						m[i, j] = Math.Min(
							m[i, j],
							m[i - 2, j - 2] + diff
							);
					}
				}
			return m;
		}

		public Tuple<string, string> GetMisspelling()
		{
			var minLen = Math.Min(dictionaryWord.Length, word.Length);
			var i = 0;
			while (i < minLen && dictionaryWord[i] == word[i])
			{
				i++;
			}
			if (i == minLen)
			{
				string s1 = "", s2 = "";
				if (dictionaryWord.Length > word.Length)
					s1 = "" + dictionaryWord[i];
				else if (dictionaryWord.Length < word.Length)
					s2 = "" + word[i];
				return Tuple.Create(s1, s2);
			}

			if (i + 1 < dictionaryWord.Length && dictionaryWord[i + 1] == word[i])
			{
				if (i + 1 < word.Length && dictionaryWord[i] == word[i+1])
				{
					return Tuple.Create(("" + dictionaryWord[i]) + dictionaryWord[i + 1], ("" + word[i]) + word[i + 1]);
				}
				return Tuple.Create("" + dictionaryWord[i], "");
			}
			if (i + 1 < word.Length && dictionaryWord[i] == word[i + 1])
				return Tuple.Create("", "" + word[i]);

			return Tuple.Create("" + dictionaryWord[i], "" + word[i]);
		}
	}

    [TestFixture]
    internal class LevensteinInfoTest
    {
        [Test, Explicit]
        public static void FixWordTest()
        {
            throw new NotImplementedException();
        }

        [Test]
        [TestCase("ацитил", "ацетил", 1)]
        [TestCase("ацтиил", "ацетил", 2)]
        [TestCase("почему", "почиму", 1)]
        [TestCase("why", "what", 2)]
        [TestCase("why", "wyh", 1)]
        [TestCase("why", "hwy", 1)]
        public void TestLevenstein(string s1, string s2, int answer)
        {
            var levensteinInfo = new LevensteinInfo(s1, s2);
            Assert.AreEqual(answer, levensteinInfo.GetDistance());
        }

        [Test]
        [TestCase("ацитил", "ацетил", "и", "е")]
        [TestCase("цаетил", "ацетил", "ца", "ац")]
        [TestCase("почему", "почиму", "е", "и")]
        [TestCase("what", "hat", "w", "")]
        [TestCase("wght", "what", "g", "")]
        [TestCase("why", "whyt", "", "t")]
        public void TestGettingMisspellings(string s1, string s2, string c1, string c2)
        {
            var levensteinInfo = new LevensteinInfo(s1, s2);
            var misspelling = levensteinInfo.GetMisspelling();
            Assert.AreEqual(misspelling.Item1, c1);
            Assert.AreEqual(misspelling.Item2, c2);
        }
    }
}