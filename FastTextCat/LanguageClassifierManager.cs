using FastTextCat.NaiveBayes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FastTextCat
{
    public class LanguageClassifierManager
    {
        public int MaxNGramLength { get; }
        public int MaxFeaturesToEvaluate { get; }
        public int MaxSizeOfDistribution { get; }
        public int OccurrenceNumberThreshold { get; }
        public int OnlyReadFirstNLines { get; }
        /// <summary>
        /// true if it is allowed to use more than one thread for training
        /// </summary>
        public bool AllowUsingMultipleThreadsForTraining { get; private set; }

        public LanguageClassifierManager()
            : this(5, 1000, 4000, 0, int.MaxValue)
        { }

        public LanguageClassifierManager(int maxNGramLength,
            int maxFeaturesToEvaluate,
            int maxSizeOfDistribution, 
            int occuranceNumberThreshold, 
            int onlyReadFirstNLines, 
            bool allowUsingMultipleThreadsForTraining = true)
        {
            MaxNGramLength = maxNGramLength;
            MaxFeaturesToEvaluate = maxFeaturesToEvaluate;
            MaxSizeOfDistribution = maxSizeOfDistribution;
            OccurrenceNumberThreshold = occuranceNumberThreshold;
            OnlyReadFirstNLines = onlyReadFirstNLines;
            AllowUsingMultipleThreadsForTraining = allowUsingMultipleThreadsForTraining;
        }

        public LanguageClassifier CreateLanguageClassifierFromLanguageModels(IEnumerable<LanguageModel> languageModels)
        {
            return new LanguageClassifier(languageModels, MaxNGramLength, MaxFeaturesToEvaluate, OnlyReadFirstNLines);
        }

        public LanguageClassifier TrainLanguageModelsAndCreateClassifier(IEnumerable<Tuple<LanguageInfo, TextReader>> input)
        {
            var languageModels = TrainLanguageModels(input).ToList();

            return CreateLanguageClassifierFromLanguageModels(languageModels);
        }

        public IEnumerable<LanguageModel> TrainLanguageModels(IEnumerable<Tuple<LanguageInfo, TextReader>> input)
        {
            if (AllowUsingMultipleThreadsForTraining)
            {
                return input
                    .AsParallel()
                    .AsOrdered()
                    .Select(languageAndText =>
                    {
                        return trainLanguageModel(languageAndText.Item1, languageAndText.Item2);
                    });
            }

            return input
                .Select(languageAndText =>
                {
                    return trainLanguageModel(languageAndText.Item1, languageAndText.Item2);
                });
        }

        private LanguageModel trainLanguageModel(LanguageInfo languageInfo, TextReader text)
        {
            IEnumerable<string> tokens = new CharacterNGramGenerator(MaxNGramLength, OnlyReadFirstNLines).GetFeatures(text);
            IDistribution<string> distribution = createLanguageModel(tokens, OccurrenceNumberThreshold, MaxSizeOfDistribution);

            return new LanguageModel(distribution, languageInfo);
        }

        public void SaveLanguageModels(IEnumerable<LanguageModel> languageModels, string outputFilePath)
        {
            using (var file = File.OpenWrite(outputFilePath))
            {
                SaveLanguageModels(languageModels, file);
            }
        }

        public void SaveLanguageModels(IEnumerable<LanguageModel> languageModels, Stream outputStream)
        {
            XmlLanguageModelsPersister.Save(languageModels, MaxSizeOfDistribution, MaxNGramLength, outputStream);
        }

        public void TrainAndSaveLanguageModels(IEnumerable<Tuple<LanguageInfo, TextReader>> input, string outputFilePath)
        {
            using (var file = File.OpenWrite(outputFilePath))
            {
                TrainAndSaveLanguageModels(input, file);
            }
        }

        public void TrainAndSaveLanguageModels(IEnumerable<Tuple<LanguageInfo, TextReader>> input, Stream outputStream)
        {
            var languageModels = TrainLanguageModels(input).ToList();

            SaveLanguageModels(languageModels, outputStream);
        }

        public LanguageClassifier LoadLanguageModelsAndCreateClassifier(string inputFilePath, Func<LanguageModel, bool>? filterPredicate = null)
        {
            using (var file = File.OpenRead(inputFilePath))
            {
                return LoadLanguageModelsAndCreateClassifier(file, filterPredicate);
            }
        }

        public LanguageClassifier LoadLanguageModelsAndCreateClassifier(Stream inputStream, Func<LanguageModel, bool>? filterPredicate = null)
        {
            filterPredicate = filterPredicate ?? (_ => true);
            int maxNGramLength;
            int maximumSizeOfDistribution;
            var languageModelList = XmlLanguageModelsPersister
                    .Load(inputStream, out maximumSizeOfDistribution, out maxNGramLength)
                    .Where(filterPredicate);

            return CreateLanguageClassifierFromLanguageModels(languageModelList);
        }

        private static IDistribution<string> createLanguageModel(IEnumerable<string> tokens, int minOccuranceNumberThreshold, int maxTokensInDistribution)
        {
            IModifiableDistribution<string> distribution = new Distribution<string>();

            distribution.AddEventRange(tokens);

            if (minOccuranceNumberThreshold > 0)
            {
                distribution.PruneByCount(minOccuranceNumberThreshold);
            }

            distribution.PruneByRank(maxTokensInDistribution);

            return distribution;
        }
    }
}
