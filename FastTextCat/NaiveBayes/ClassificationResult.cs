using System;
using System.Linq;

namespace FastTextCat.NaiveBayes
{
    public class ClassificationResult<TCategory>
    {
        public TCategory Category { get; }

        public double Score { get; set; }

        public ClassificationResult(TCategory category, double score)
        {
            Category = category;
            Score = score;
        }
    }
}
