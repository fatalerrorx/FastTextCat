using FastTextCat.NaiveBayes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FastTextCat
{
    public class LanguageModel
    {
        public LanguageInfo Language { get; }

        public IDictionary<string, string> Metadata { get; }
        
        public IDistribution<string> Features { get; }

        public LanguageModel(IDistribution<string> features, LanguageInfo language)
        {
            Language = language;
            Features = features;
            Metadata = new Dictionary<string, string>();
        }

        public LanguageModel(IDistribution<string> features, LanguageInfo language, IDictionary<string, string> metadata)
        {
            Language = language;
            Metadata = metadata;
            Features = features;
        }
    }
}
