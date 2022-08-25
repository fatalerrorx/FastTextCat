using FastTextCat.NaiveBayes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FastTextCat.Util;

namespace FastTextCat
{
    /// <summary>
    /// Extracts char-ngrams out of TextReader, char[] or string.
    /// </summary>
    public class CharacterNGramGenerator : IFeatureExtractor<TextReader, string>, IFeatureExtractor<char[], string>, IFeatureExtractor<string, string> 
    {
        private const char Seperator = '_';
        private const int MinRollingNGramBufferSize = 64;

        private readonly int _maxNGramLength = 5;
        private readonly long _maxLinesToRead;

        private readonly char[] _letterNGramBuffer;

        public CharacterNGramGenerator(int maxNGramLength, long maxLinesToRead = long.MaxValue)
        {
            if (maxNGramLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxNGramLength), "Must be positive integer number.");
            }

            _maxNGramLength = maxNGramLength;
            _maxLinesToRead = maxLinesToRead;

            _letterNGramBuffer = new char[maxNGramLength];
        }

        /// <summary>
        /// Splits text into tokens, transforms each "token" into "_token_" (prepends and appends underscores) 
        /// and then extracts proper ngrams out of each "_token_".
        /// </summary>
        /// <param name="text"></param>
        /// <returns>the sequence of ngrams extracted</returns>
        public IEnumerable<string> GetFeatures(string text)
        {
            return getFeatures(text);
        }

        /// <summary>
        /// Splits text into tokens, transforms each "token" into "_token_" (prepends and appends underscores) 
        /// and then extracts proper ngrams out of each "_token_".
        /// </summary>
        /// <param name="text"></param>
        /// <returns>the sequence of ngrams extracted</returns>
        public IEnumerable<string> GetFeatures(char[] text)
        {
            return getFeatures(text);
        }

        /// <summary>
        /// Splits text into tokens, transforms each "token" into "_token_" (prepends and appends underscores)  and then
        /// extracts proper ngrams out of each "_token_".
        /// </summary>
        /// <param name="textReader"></param>
        /// <returns>the sequence of ngrams extracted</returns>
        public IEnumerable<string> GetFeatures(TextReader textReader)
        {
            var characters = textReader.StreamCharacters();
            return getFeatures(characters);
        }

        private IEnumerable<string> getFeatures(IEnumerable<char> characters)
        {
            char[] rollingNGramBuffer = new char[Math.Max(_maxNGramLength * 2, MinRollingNGramBufferSize)];
            int rollingNGramBufferPos = 0;
            int rollingNGramBufferCharCount = 0;

            bool clean = false;
            bool insideWord = false;
            
            int numberOfLinesRead = 0;
            char previousCharacter = default;

            foreach (var currentCharacter in characters)
            {
                if (currentCharacter == '\r' || currentCharacter == '\n' && previousCharacter != '\r')
                {
                    numberOfLinesRead++;
                }
                if (numberOfLinesRead >= _maxLinesToRead)
                {
                    break;
                }

                if (insideWord)
                {
                    if (!isSeparator(currentCharacter))
                    {
                        appendCharacter(char.ToLower(currentCharacter));
                    }
                    else
                    {
                        insideWord = false;
                        clean = true;

                        appendCharacter(Seperator);
                    }

                }
                else
                {
                    if (!isSeparator(currentCharacter))
                    {
                        insideWord = true;

                        appendCharacter(Seperator);
                        appendCharacter(char.ToLower(currentCharacter));
                    }
                }

                IEnumerable<string> ngrams = computeNGrams(rollingNGramBuffer, rollingNGramBufferPos, rollingNGramBufferCharCount);
                foreach (var ngram in ngrams)
                {
                    yield return ngram;
                }

                if (clean)
                {
                    rollingNGramBufferPos = 0;
                    rollingNGramBufferCharCount = 0;

                    clean = false;
                }

                previousCharacter = currentCharacter;
            }

            if (insideWord)
            {
                appendCharacter(Seperator);

                IEnumerable<string> ngrams = computeNGrams(rollingNGramBuffer, rollingNGramBufferPos, rollingNGramBufferCharCount);
                foreach (var ngram in ngrams)
                {
                    yield return ngram;
                }
            }

            void appendCharacter(char character)
            {
                rollingNGramBuffer[rollingNGramBufferPos] = character;

                rollingNGramBufferPos = (rollingNGramBufferPos + 1) % rollingNGramBuffer.Length;
                rollingNGramBufferCharCount = Math.Min(rollingNGramBufferCharCount + 1, _maxNGramLength);
            }
        }

        private IEnumerable<string> computeNGrams(char[] rollingNGramBuffer, int afterLastCharacterPos, int noCharacters)
        {
            int i;
            if (afterLastCharacterPos >= noCharacters)
            {
                for (i = 0; i < noCharacters; i++)
                {
                    int start = afterLastCharacterPos - 1 - i;
                    if (i != 0 || rollingNGramBuffer[start] != Seperator)
                    {
                        yield return new string(rollingNGramBuffer, start, i + 1);
                    }
                }
            }
            else
            {
                for (i = noCharacters - 1; i >= 0; i--)
                {
                    int relativeRollingNGramBufferPos = afterLastCharacterPos - 1 - i;
                    int absoluteRollingNGramBufferPos = relativeRollingNGramBufferPos < 0 ? relativeRollingNGramBufferPos * -1 : relativeRollingNGramBufferPos;

                    _letterNGramBuffer[noCharacters - 1 - i] = rollingNGramBuffer[absoluteRollingNGramBufferPos];
                }

                for (i = 0; i < noCharacters; i++)
                {
                    int start = noCharacters - 1 - i;
                    if (i != 0 || _letterNGramBuffer[start] != Seperator)
                    {
                        yield return new string(_letterNGramBuffer, start, i + 1);
                    }
                }
            }
        }

        private static bool isSeparator(char b)
        {
            return char.IsLetter(b) == false;
        }
    }
}
