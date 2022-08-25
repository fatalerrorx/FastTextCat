using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using FastTextCat.NaiveBayes;

namespace FastTextCat.Test
{
    internal class Program
    {
        private static string DataDirectoryPath = Path.Combine(".", "Data");

        private const string TrainingDataText = "x_train.txt";
        private const string TrainginDataLanguage = "y_train.txt";

        private const string TestingDataText = "x_test.txt";
        private const string TestingDataLanguage = "y_test.txt";

        static void Main(string[] args)
        {
            var identifier = train();
            //var manager = new LanguageClassifierManager();
            //var identifier = manager.LoadLanguageModelsAndCreateClassifier("WiLI-2018.xml");

            testAccuracy(identifier);
            
            Console.ReadLine();
        }

        private static void testAccuracy(LanguageClassifier identifier)
        {
            var testingDataTextLines = File.ReadLines(Path.Combine(DataDirectoryPath, TestingDataText));
            var testingDataLanguageLines = File.ReadLines(Path.Combine(DataDirectoryPath, TestingDataLanguage));
            var stopWatch = new Stopwatch();

            const int sampleInterval = 1000;

            int total = 0;
            int correct = 0;
            foreach (var testingDataLine in testingDataLanguageLines.Zip(testingDataTextLines))
            {
                var result = identifier.Identify(testingDataLine.Second);
                ClassificationResult<LanguageInfo> classificationResult = result.First();
                if (classificationResult.Category.Iso639_2T == testingDataLine.First)
                {
                    correct++;
                }
                
                total++;

                if (total % sampleInterval == 0 && total > 0)
                {
                    Console.WriteLine($"Percent correct: {correct / (double)total * 100}");

                    if (stopWatch.IsRunning)
                    {
                        stopWatch.Stop();
                        Console.WriteLine($"Classifications per second: {1000 / (stopWatch.ElapsedMilliseconds / (double)sampleInterval)}");
                    }

                    stopWatch.Restart();
                }
            }

            Console.WriteLine($"Percent correct: {correct / (double)total * 100}");
        }

        private static LanguageClassifier train()
        {
            var trainingDataTextLines = File.ReadLines(Path.Combine(DataDirectoryPath, TrainingDataText));
            var trainingDataLanguageLines = File.ReadLines(Path.Combine(DataDirectoryPath, TrainginDataLanguage));
            var trainingData = new Dictionary<string, StringBuilder>();

            foreach (var traingingDataLine in trainingDataLanguageLines.Zip(trainingDataTextLines))
            {
                StringBuilder? stringBuilder;

                if (!trainingData.TryGetValue(traingingDataLine.First, out stringBuilder))
                {
                    stringBuilder = new StringBuilder();
                    trainingData.Add(traingingDataLine.First, stringBuilder);
                }

                stringBuilder.AppendLine(traingingDataLine.Second);
            }

            var manager = new LanguageClassifierManager();
            var input = trainingData
                .Select(f =>
                {
                    LanguageInfo languageInfo = new LanguageInfo(f.Key, "", "", "");
                    TextReader textReader = new StringReader(f.Value.ToString());

                    return Tuple.Create(languageInfo, textReader);
                });

            manager.TrainAndSaveLanguageModels(input, "WiLI-2018.xml");
            return manager.LoadLanguageModelsAndCreateClassifier("WiLI-2018.xml");
        }
    }
}
