using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FastTextCat.Util
{
    public static class TextReaderExtensions
    {
        public static void SkipLines(this TextReader textReader, int lineCount)
        {
            for (int i = 0; i < lineCount; i++)
            {
                textReader.ReadLine();
            }
        }

        public static IEnumerable<char> StreamCharacters(this TextReader text)
        {
            var textBuffer = new char[4096];
            int charactersRead;

            while ((charactersRead = text.Read(textBuffer, 0, textBuffer.Length)) > 0)
            {
                for (int i = 0; i < charactersRead; i++)
                {
                    yield return textBuffer[i];
                }
            }
        }
    }
}
