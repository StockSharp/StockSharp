#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: XMLCommToHTM.DocViewer
File: GenerateHtml_Solution.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace XMLCommToHTM
{
	using System.Linq;
	using System.Xml.Linq;

	using XMLCommToHTM.DOM;

	partial class GenerateHtml
	{
		static string Generate(SolutionDom sln)
		{
			var doc = GetDoc(out var body);

			body.Add(x("h1", sln.Name));

			XElement tbody;

			var table = Section(
				Names[Strings.Namespace + "_s"],
				x("table", a("class", "doc_table"), tbody = x("tbody"))
			);

			tbody.Add(BuildRow(new XElement[0], x("span", Names[Strings.Name]), Names[Strings.Description].ToSpan(), "th"));

			foreach (var ns in sln.Namespaces)
				tbody.Add(BuildRow(
					Enumerable.Empty<XElement>(),
					BuildNsUrl(ns.Name),
					XMLUtils.GetTagInnerXml(ns.DocInfo, "summary", Navigation, false)
					));

			body.Add(table);

			return doc.ToString();
		}
	}
}