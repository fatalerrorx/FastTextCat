﻿using System;
using System.Diagnostics;
using System.Linq;

namespace FastTextCat
{
    [DebuggerDisplay("{ToString()}")]
    public class LanguageInfo
    {
        /// <summary>
        /// A code of the language according to ISO639-2 (Part2T)
        /// </summary>
        public string Iso639_2T { get; }
        public string Iso639_3 { get; }
        public string EnglishName { get; }
        public string LocalName { get; }

        public LanguageInfo(string iso6392T, string iso6393, string englishName, string localName)
        {
            Iso639_2T = iso6392T;
            Iso639_3 = iso6393;
            EnglishName = englishName;
            LocalName = localName;
        }

        public override string ToString()
        {
            return $"ISO639-2-T: {Iso639_2T}, ISO639-3: {Iso639_3}, EnglishName: {EnglishName}, LocalName: {LocalName}";
        }
    }
}
