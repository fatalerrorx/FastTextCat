# FastTextCat

| License |
| --------|
|[![License](https://img.shields.io/github/license/fatalerrorx/FastTextCat.svg)](https://github.com/fatalerrorx/FastTextCat/blob/master/license.MIT)|

## Why FastTextCat?
- *FastTextCat* is a fork of NTextCat that has had a complete rewrite. Why? To make it faster and more accurate. The result is a 15x speed up over the original, better recognition accuracy and cleaner code.
- *FastTextCat* helps to recognize (identify) the language of a given text (e.g. read a sentence and say it is *Italian*).

## How to use
*FastTextCat* supports .NET core 6.0

```csharp
using FastTextCat;
...
// Don't forget to deploy a language profile (e.g. WiLI-2018.xml) with your application.
var manager = new LanguageClassifierManager();
var identifier = manager.LoadLanguageModelsAndCreateClassifier("WiLI-2018.xml");
var result = identifier.Identify("your text to get its language identified");
ClassificationResult<LanguageInfo> classificationResult = result.First();
Console.WriteLine("The language of the text is '{0}' (ISO639-2 code)", classificationResult.Category.Iso639_2T);

// outputs: The language of the text is 'eng' (ISO639-3 code)
```
