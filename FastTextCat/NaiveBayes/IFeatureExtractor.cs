using System;
using System.Collections.Generic;
using System.Linq;

namespace FastTextCat.NaiveBayes
{
    internal interface IFeatureExtractor<TSource, TFeature>
    {
        IEnumerable<TFeature> GetFeatures(TSource obj);
    }
}
