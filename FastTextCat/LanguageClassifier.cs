using FastTextCat.NaiveBayes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FastTextCat
{
    public class LanguageClassifier
    {
        private readonly NaiveBayesClassifier<IEnumerable<string>, string, LanguageInfo> _classifier;

        public int MaxNGramLength { get; }

        public int MaxFeaturesToEvaluate { get; }

        public int OnlyReadFirstNLines { get; }

        public LanguageClassifier(IEnumerable<LanguageModel> languageModels, int maxNGramLength, int maxFeaturesToEvaluate, int onlyReadFirstNLines)
        {
            MaxNGramLength = maxNGramLength;
            MaxFeaturesToEvaluate = maxFeaturesToEvaluate;
            OnlyReadFirstNLines = onlyReadFirstNLines;

            var distributionsByCategory = languageModels.ToDictionary(lm => lm.Language, lm => lm.Features);
            _classifier = new NaiveBayesClassifier<IEnumerable<string>, string, LanguageInfo>(distributionsByCategory, maxFeaturesToEvaluate);
        }

        public IEnumerable<ClassificationResult<LanguageInfo>> Identify(string text)
        {
            var extractor = new CharacterNGramGenerator(MaxNGramLength, OnlyReadFirstNLines);
            IEnumerable<string> tokens = extractor.GetFeatures(text);

            return _classifier.Classify(tokens);
        }
    }
}
