using System;
using System.Collections.Generic;
using System.Linq;

namespace FastTextCat.NaiveBayes
{
    internal interface ICategorizedClassifier<TItem, TCategory>
    {
        IEnumerable<ClassificationResult<TCategory>> Classify(TItem item);
    }
}
