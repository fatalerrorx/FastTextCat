using System;
using System.Collections.Generic;
using System.Linq;

namespace FastTextCat.NaiveBayes
{
    internal class NaiveBayesClassifier<TItem, TFeature, TCategory> : ICategorizedClassifier<TItem, TCategory>
        where TItem : IEnumerable<TFeature>
        where TFeature : notnull
    {
        private class DistributionCategory
        {
            public TCategory Category { get; }

            public double UnrepresentedFeatureLogProbability { get; }

            public double CategoryLogProbability { get; }

            public DistributionCategory(TCategory category, double unrepresentedFeatureLogProbability, double categoryLogProbability)
            {
                Category = category;
                UnrepresentedFeatureLogProbability = unrepresentedFeatureLogProbability;
                CategoryLogProbability = categoryLogProbability;
            }
        }

        private const double LaplaceSmoothingFactor = 1;

        /// <summary>
        /// Maximum number of features to use for classification. Using to many can severly decrease peformance
        /// while not improving classification accuracy by much
        /// </summary>
        private int _maxFeatures;

        private readonly Dictionary<TFeature, double[]> _featureLogProbabilityMatrix;
        private readonly DistributionCategory[] _distributionCategories;

        public NaiveBayesClassifier(IDictionary<TCategory, IDistribution<TFeature>> distributionsByCategory, int maxFeatures)
        {
            if (distributionsByCategory == null)
            {
                throw new ArgumentNullException(nameof(distributionsByCategory));
            }

            _maxFeatures = maxFeatures;

            _distributionCategories = new DistributionCategory[distributionsByCategory.Count];
            _featureLogProbabilityMatrix = new Dictionary<TFeature, double[]>();

            initialize(distributionsByCategory, _distributionCategories, _featureLogProbabilityMatrix);
        }

        private static void initialize(IDictionary<TCategory, IDistribution<TFeature>> distributionsByCategory, 
            DistributionCategory[] distributionCategories, 
            Dictionary<TFeature, double[]> featureLogProbabilityMatrix)
        {
            long globalTotalEventCountWithNoise = distributionsByCategory.Sum(d => d.Value.TotalEventCountWithNoise);
            int categoryCount = distributionCategories.Length;
            int categoryIndex = 0;

            foreach (var categoryAndDistributionKvp in distributionsByCategory)
            {
                TCategory category = categoryAndDistributionKvp.Key;
                IDistribution<TFeature> distribution = categoryAndDistributionKvp.Value;

                //This is the denominator for computing the feature probability with the laplace smoothing factor to avoid the zero probability problem.
                double totalEventCountWithNoiseWithLaplaceSmoothingLog = Math.Log(distribution.TotalEventCountWithNoise + 1 * LaplaceSmoothingFactor);
                //The initial classification score value is the probability of the category log(A / B) = log(A) - log(B)
                double categoryLogProbability = Math.Log(distribution.TotalEventCountWithNoise) - Math.Log(globalTotalEventCountWithNoise);
                //(0 + LaplaceSmoothingFactor) / (LaplaceSmoothingFactor + 1) * TotalEventCountWithNoise
                //log(LaplaceSmoothingFactor) - log((LaplaceSmoothingFactor + 1) * TotalEventCountWithNoise)
                double unrepresentedFeatureLogProbability = Math.Log(LaplaceSmoothingFactor) - totalEventCountWithNoiseWithLaplaceSmoothingLog;

                distributionCategories[categoryIndex] = new DistributionCategory(category, unrepresentedFeatureLogProbability, categoryLogProbability);

                foreach (var featureAndCountKvp in distribution)
                {
                    TFeature feature = featureAndCountKvp.Key;
                    long featureCount = featureAndCountKvp.Value;

                    double[] featureLogProbabilities = getOrCreateFeatureLogProbabilitiesArray(featureLogProbabilityMatrix, feature, categoryCount);

                    featureLogProbabilities[categoryIndex] = Math.Log(featureCount + LaplaceSmoothingFactor) - totalEventCountWithNoiseWithLaplaceSmoothingLog;
                }

                categoryIndex++;
            }

            // Fill all the gaps in the feature probability arrays with the UnrepresentedFeatureLogProbability as
            // the gap means the feature was not present in the category distribution but should still have a certain
            // probability since we are using laplace smoothing
            foreach (var featureProbabilityLogs in featureLogProbabilityMatrix.Values)
            {
                for (int i = 0; i < distributionCategories.Length; i++)
                {
                    if (double.IsNaN(featureProbabilityLogs[i]))
                    {
                        featureProbabilityLogs[i] = distributionCategories[i].UnrepresentedFeatureLogProbability;
                    }
                }
            }
        }

        private static double[] getOrCreateFeatureLogProbabilitiesArray(Dictionary<TFeature, double[]> featureLogProbabilityMatrix, TFeature feature, int categoryCount)
        {
            double[]? featureLogProbabilities;

            if (!featureLogProbabilityMatrix.TryGetValue(feature, out featureLogProbabilities))
            {
                featureLogProbabilities = new double[categoryCount];

                Array.Fill(featureLogProbabilities, double.NaN);

                featureLogProbabilityMatrix.Add(feature, featureLogProbabilities);
            }

            return featureLogProbabilities;
        }

        public IEnumerable<ClassificationResult<TCategory>> Classify(TItem features)
        {
            var featureAndCounts = groupFeatures(features, _maxFeatures);
            var classificationScores = new ClassificationResult<TCategory>[_distributionCategories.Length];
            
            for (int i = 0; i < _distributionCategories.Length; i++)
            {
                DistributionCategory distributionCategory = _distributionCategories[i];
                classificationScores[i] = new ClassificationResult<TCategory>(distributionCategory.Category, distributionCategory.CategoryLogProbability);
            }

            foreach (var featureAndCount in featureAndCounts)
            {
                double[]? featureProbabilityLogs;
                if (!_featureLogProbabilityMatrix.TryGetValue(featureAndCount.Key, out featureProbabilityLogs))
                {
                    for(int i = 0; i < _distributionCategories.Length; i++)
                    {
                        unchecked
                        {
                            classificationScores[i].Score += _distributionCategories[i].UnrepresentedFeatureLogProbability * featureAndCount.Value;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < featureProbabilityLogs.Length; i++)
                    {
                        var featureProbabilityLog = featureProbabilityLogs[i];
                        unchecked
                        {
                            classificationScores[i].Score += featureProbabilityLog * featureAndCount.Value;
                        }
                    }
                }
            }

            return classificationScores.OrderByDescending(t => t.Score);
        }

        private static IReadOnlyDictionary<TFeature, int> groupFeatures(TItem features, int maxFeatures)
        {
            var groupedFeatures = new Dictionary<TFeature, int>();
            foreach (var feature in features.Take(maxFeatures))
            {
                int count;
                if(groupedFeatures.TryGetValue(feature, out count))
                {
                    groupedFeatures[feature] = count + 1;
                }
                else
                {
                    groupedFeatures[feature] = 1;
                }
            }
            return groupedFeatures;
        }
    }
}
