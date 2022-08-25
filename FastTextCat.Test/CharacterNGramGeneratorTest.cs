using System.Collections.Generic;
using NUnit.Framework;

namespace FastTextCat.Test
{
    public class CharacterNGramGeneratorTest
    {
        private readonly string[] expectedNgrams = new[]
        {
            "t", "_t", "h", "th", "_th", "e", "he", "the", "_the", "e_", "he_", "the_", "_the_",
            "q", "_q", "u", "qu", "_qu", "i", "ui", "qui", "_qui", "c", "ic", "uic", "quic", "_quic", "k", "ck", "ick", "uick", "quick", "k_", "ck_", "ick_", "uick_",
            "b", "_b", "r", "br", "_br", "o", "ro", "bro", "_bro", "w", "ow", "row", "brow", "_brow", "n", "wn", "own", "rown", "brown", "n_", "wn_", "own_", "rown_",
            "f", "_f", "o", "fo", "_fo", "x", "ox", "fox", "_fox", "x_", "ox_", "fox_", "_fox_"
        };

        [Test]
        public void Test()
        {
            var ngrams = new HashSet<string>(new CharacterNGramGenerator(5).GetFeatures("The quick brown fox"));
            Assert.True(ngrams.SetEquals(expectedNgrams));
        }

        [Test]
        public void TestMaxLinesToRead()
        {
            var ngrams = new HashSet<string>(new CharacterNGramGenerator(1, 5).GetFeatures("abcdef\rghjjk\nlmn\nopq\r\nrstu\r\nvwxyz"));
            
            foreach (char ngram in "abcdefghjjklmnopqrstu")
            {
                Assert.True(ngrams.Contains(ngram.ToString()));
            }

            foreach (char ngram in "vwxyz")
            {
                Assert.False(ngrams.Contains(ngram.ToString()));
            }
        }
    }
}
