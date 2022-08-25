using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace FastTextCat
{
    internal class XmlLanguageModelsPersister
    {
        private const string MaximumSizeOfDistributionElement = "MaximumSizeOfDistribution";
        private const string MaxNGramLengthElement = "MaxNGramLength";
        private const string LanguageIdentificationProfileElement = "LanguageIdentificationProfile";
        private const string ParametersElement = "Parameters";
        private const string LanguageModelsElement = "LanguageModels";

        public static void Save(IEnumerable<LanguageModel> languageModels, int maximumSizeOfDistribution, int maxNGramLength, Stream outputStream)
        {
            LanguageModel[] languageModelsLocalCopy = languageModels.ToArray();
            var languageFieldGetters = new Dictionary<string, Func<LanguageInfo, string>>
            {
                { "ISO 639-2-T", lm => lm.Iso639_2T },
                { "ISO 639-3", lm => lm.Iso639_3 },
                { "English name", lm => lm.EnglishName },
                { "local name", lm => lm.LocalName },
                { "any name available", lm => lm.Iso639_2T ?? lm.Iso639_3 ?? lm.EnglishName ?? lm.LocalName },
            };

            KeyValuePair<string, List<string>> languageNames = languageFieldGetters
                .ToDictionary(kvp => kvp.Key, kvp => languageModelsLocalCopy.Select(lm => kvp.Value(lm.Language)).ToList())
                .FirstOrDefault(kvp => kvp.Value.All(name => string.IsNullOrWhiteSpace(name)));

            XComment xComment =
                string.IsNullOrEmpty(languageNames.Key)
                ? new XComment("WARNING! Some of the language model(s) do(es)n't have any language name assigned")
                : new XComment($"Contains models for the following languages (by {languageNames.Key}): {(string.Join(", ", languageNames.Value))}");

            XmlLanguageModelPersister persister = new XmlLanguageModelPersister();
            XDocument xDoc = new XDocument(
                xComment, 
                new XElement(
                    LanguageIdentificationProfileElement, 
                    new XElement(
                        ParametersElement, 
                        new XElement(MaximumSizeOfDistributionElement, maximumSizeOfDistribution), 
                        new XElement(MaxNGramLengthElement, maxNGramLength)), 
                    new XElement(LanguageModelsElement, languageModelsLocalCopy.Select(persister.ToXml))));

            XmlWriter xmlWriter = XmlWriter.Create(outputStream, new XmlWriterSettings { Indent = true });
            xDoc.WriteTo(xmlWriter);
            xmlWriter.Flush();
        }

        public static IEnumerable<LanguageModel> Load(Stream sourceStream, out int maximumSizeOfDistribution, out int maxNGramLength)
        {
            XDocument xDocument = XDocument.Load(sourceStream);
            XElement? rootElement = xDocument.Root;
            if(rootElement ==  null)
            {
                throw new InvalidOperationException("Document root missing");
            }
            IEnumerable<LanguageModel> result = Load(rootElement, out maximumSizeOfDistribution, out maxNGramLength);
            return result;
        }

        public static IEnumerable<LanguageModel> Load(XElement xProfile, out int maximumSizeOfDistribution, out int maxNGramLength)
        {
            if(xProfile.Name != LanguageIdentificationProfileElement)
            {
                throw new ArgumentException("XML root is not " + LanguageIdentificationProfileElement, nameof(xProfile));
            }

            XElement? xParameters = xProfile.Element(ParametersElement);
            if(xParameters == null)
            {   
                throw new InvalidOperationException($"Element #{ParametersElement} missing");
            }

            XElement? xMaximumSizeOfDistribution = xParameters.Element(MaximumSizeOfDistributionElement);
            if(xMaximumSizeOfDistribution == null)
            {   
                throw new InvalidOperationException($"Element #{MaximumSizeOfDistributionElement} missing");
            }
            maximumSizeOfDistribution = int.Parse(xMaximumSizeOfDistribution.Value);
            
            XElement? xMaxNGramLength = xParameters.Element(MaxNGramLengthElement);
            if(xMaxNGramLength == null)
            {   
                throw new InvalidOperationException($"Element #{MaxNGramLengthElement} missing");
            }
            maxNGramLength = int.Parse(xMaxNGramLength.Value);

            XElement? xLanguageModels = xProfile.Element(LanguageModelsElement);
            if(xLanguageModels == null)
            {   
                throw new InvalidOperationException($"Element #{LanguageModelsElement} missing");
            }
            
            XmlLanguageModelPersister persister = new XmlLanguageModelPersister();
            List<LanguageModel> languageModelList = xLanguageModels
                .Elements(XmlLanguageModelPersister.RootElement)
                .Select(persister.Load)
                .ToList();

            return languageModelList;
        }
    }
}
