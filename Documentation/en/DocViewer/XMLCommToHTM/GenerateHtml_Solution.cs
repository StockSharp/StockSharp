namespace XMLCommToHTM
{
	using System.Linq;
	using System.Xml.Linq;

	using XMLCommToHTM.DOM;

	partial class GenerateHtml
	{
		static string Generate(SolutionDom sln)
		{
			XElement body;
			var doc = GetDoc(out body);
			
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