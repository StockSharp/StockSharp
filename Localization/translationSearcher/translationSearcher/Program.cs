using System;
using System.IO;

namespace translationSearcher
{
	class Program
	{
		static void Main(string[] args)
		{
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
