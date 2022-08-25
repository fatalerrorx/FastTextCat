using FastTextCat.NaiveBayes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace FastTextCat
{
    internal class XmlLanguageModelPersister
    {
        public const string RootElement = "LanguageModel";
        private const string MetadataElement = "metadata";
        private const string NGramsElement = "ngrams";
        private const string NGramElement = "ngram";
        private const string TotalNoiseCountAtribute = "totalNoiseCount";
        private const string DistinctNoiseCountAtribute = "distinctNoiseCount";
        private const string TextAttribute = "text";
        private const string CountAttribute = "count";
        private const string LanguageElement = "Language";
        private const string LanguageIso639_2T_Attribute = "ISO639-2T";
        private const string LanguageIso639_3_Attribute = "ISO639-3";
        private const string LanguageEnglishNameAttribute = "EnglishName";
        private const string LanguageLocalNameAttribute = "LocalName";
        
        public LanguageModel Load(Stream sourceStream)
        {
            XDocument xDocument = XDocument.Load(sourceStream);
            XElement? rootElement = xDocument.Root;
            if(rootElement ==  null){
                throw new InvalidOperationException("Document root missing");
            }
            LanguageModel result = Load(rootElement);
            return result;
        }

        public LanguageModel Load(XElement xLanguageModel)
        {
            XElement? xMetadata = xLanguageModel.Element(MetadataElement);
            if(xMetadata == null){
                throw new InvalidOperationException($"Element #{MetadataElement} missing");
            }
            
            Dictionary<string, string> metadata = xMetadata
                .Elements()
                .ToDictionary(el => el.Name.ToString(), el => el.Value);

            XElement? xLanguage = xLanguageModel.Element(LanguageElement);
            if(xLanguage == null){
                throw new InvalidOperationException($"Element #{LanguageElement} missing");
            }

            XAttribute? xIso639_2T = xLanguage.Attribute(LanguageIso639_2T_Attribute);
            string iso639_2T = xIso639_2T == null ? "" : xIso639_2T.Value;

            XAttribute? xIso639_3 = xLanguage.Attribute(LanguageIso639_3_Attribute);
            string iso639_3 = xIso639_3 == null ? "" : xIso639_3.Value;

            XAttribute? xEnglishName = xLanguage.Attribute(LanguageEnglishNameAttribute);
            string englishName = xEnglishName == null ? "" : xEnglishName.Value;

            XAttribute? xLocalName = xLanguage.Attribute(LanguageLocalNameAttribute);
            string localName = xLocalName == null ? "" : xLocalName.Value;

            LanguageInfo language = new LanguageInfo(iso639_2T, iso639_3, englishName, localName);
            var distribution = new Distribution<string>();

            XElement? xNgramsElement = xLanguageModel.Element(NGramsElement);
            if(xNgramsElement == null){
                throw new InvalidOperationException($"Element #{NGramsElement} missing");
            }

            foreach(XElement xElement in xNgramsElement.Elements(NGramElement))
            {
                XAttribute? xTextAttribute = xElement.Attribute(TextAttribute);
                if(xTextAttribute == null) {
                    throw new InvalidOperationException($"Attribute #{TextAttribute} missing");
                }
                string textAttributeValue = xTextAttribute.Value;

                XAttribute? xCountAttribute = xElement.Attribute(CountAttribute);
                if(xCountAttribute == null) {
                    throw new InvalidOperationException($"Attribute #{CountAttribute} missing");
                }
                string countAtrributeValue = xCountAttribute.Value;
                
                distribution.AddEvent(textAttributeValue, long.Parse(countAtrributeValue));
            }

            XAttribute? xTotalNoiseCountAttribute = xNgramsElement.Attribute(TotalNoiseCountAtribute);
            if(xTotalNoiseCountAttribute == null) {
                    throw new InvalidOperationException($"Attribute #{TotalNoiseCountAtribute} missing");
            }
            string totalNoiseAttributeValue = xTotalNoiseCountAttribute.Value;

            XAttribute? xDistinctNoiseCountAttribute = xNgramsElement.Attribute(DistinctNoiseCountAtribute);
            if(xDistinctNoiseCountAttribute == null) {
                    throw new InvalidOperationException($"Attribute #{DistinctNoiseCountAtribute} missing");
            }
            string distinctNoiseCountAttributeValue = xDistinctNoiseCountAttribute.Value;
            
            distribution.AddNoise(long.Parse(totalNoiseAttributeValue), long.Parse(distinctNoiseCountAttributeValue));

            return new LanguageModel(distribution, language, metadata);
        }

        public void Save(LanguageModel languageModel, Stream destinationStream)
        {
            XDocument document = new XDocument(ToXml(languageModel));
            using(XmlWriter xmlWriter = XmlWriter.Create(destinationStream, new XmlWriterSettings { Indent = true }))
            {
                document.Save(xmlWriter);
            }
        }

        public XElement ToXml(LanguageModel languageModel)
        {
            XElement languageElement = new XElement(
                LanguageElement, 
                new[]
                {
                    Tuple.Create(LanguageIso639_2T_Attribute, languageModel.Language.Iso639_2T),
                    Tuple.Create(LanguageIso639_3_Attribute, languageModel.Language.Iso639_3),
                    Tuple.Create(LanguageEnglishNameAttribute, languageModel.Language.EnglishName),
                    Tuple.Create(LanguageLocalNameAttribute, languageModel.Language.LocalName)
                }
                .Where(t => !string.IsNullOrEmpty(t.Item2))
                .Select(t => new XAttribute(t.Item1, t.Item2))
                .ToArray());

            XElement metadataElement = new XElement(
                 MetadataElement,
                 languageModel.Metadata.Select(kvp => new XElement(kvp.Key, kvp.Value)));

            XElement ngramsElement = new XElement(
                NGramsElement,
                new XAttribute(TotalNoiseCountAtribute, languageModel.Features.TotalNoiseEventsCount),
                new XAttribute(DistinctNoiseCountAtribute, languageModel.Features.DistinctNoiseEventsCount),
                languageModel.Features.Select(kvp =>
                    new XElement(
                        NGramElement,
                        new XAttribute(TextAttribute, kvp.Key),
                        new XAttribute(CountAttribute, kvp.Value))));

            return new XElement(RootElement, languageElement, metadataElement, ngramsElement);
        }
    }
}
