using System;
using System.IO;

namespace translationSearcher
{
	class Program
	{
		static void MichaelsTest()
		{
			var tr = new XmlTranslation();

			var refDir = @"d:\code\StockSharp\Localization\translationSearcher\translationSearcher\bin\Debug\StockSharp xmls";
			var textCsvFName = @"d:\code\StockSharp\Localization\translationSearcher\translationSearcher\bin\Debug\text.csv";

			foreach (var s in Directory.GetFiles(refDir, "StockSharp.*.xml"))
			{
				if (string.Compare(s, "StockSharp.Localization.xml", StringComparison.InvariantCultureIgnoreCase) == 0)
					continue;

				//var inputXmlFName = args[0];
				//var textCsvFName = args[1];
				var outputCsvFName = Path.ChangeExtension(s, "csv");
				tr.SearchTranslationsForXml(s, textCsvFName, outputCsvFName);
			}

			foreach (var s in Directory.GetFiles(refDir, "StockSharp.*.xml"))
			{
				if (string.Compare(s, "StockSharp.Localization.xml", StringComparison.InvariantCultureIgnoreCase) == 0)
					continue;

				tr.TranslateXml(s, Path.ChangeExtension(s, "csv"), textCsvFName, 1, Path.Combine(refDir, "en"));
			}
		}
		static void Main(string[] args)
		{
// 			MichaelsTest();
// 			return;

// 			var inputXmlFName = @"d:\code\translationSearcher\StockSharp xmls\a.xml";//StockSharp.Messages.xml";
// 			var outputCsvFName = Path.ChangeExtension(inputXmlFName,"csv");
// 			var textCsvFName = @"d:\code\translationSearcher\text.csv";
// 			var outputXmlFName = Path.Combine(Path.GetDirectoryName(inputXmlFName),
// 				Path.GetFileNameWithoutExtension(inputXmlFName) + "_tr.xml");

			try
			{
				var tr = new XmlTranslation();
				if (args.Length == 2)
				{
					var inputXmlFName = args[0];
					var textCsvFName = args[1];
					var outputCsvFName = Path.ChangeExtension(inputXmlFName, "csv");
					tr.SearchTranslationsForXml(inputXmlFName, textCsvFName, outputCsvFName);				
				}
				else if (args.Length == 3)
				{
					var inputXmlFName = args[0];
					var textCsvFName = args[1];
					var outputCsvFName = Path.ChangeExtension(inputXmlFName, "csv");
					var outputXmlFName = args[2];
					tr.TranslateXml(inputXmlFName, outputCsvFName, textCsvFName, 1, outputXmlFName);
				}				
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}
	}
}
