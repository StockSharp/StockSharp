#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: XMLCommToHTM.DocViewer
File: GenerateHtml_Namespace.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
using System.Linq;
using System.Xml.Linq;

using XMLCommToHTM.DOM;

namespace XMLCommToHTM
{
	using Ecng.Common;

	//namespaces page
	partial class GenerateHtml
	{
		static string Generate(NamespaceDom ns)
		{
			var doc = GetDoc(out var body);
			body.Add(
				x("h1", ns.Name + Names[Strings.SuffixDelimeter] + Names[Strings.Namespace])
				);
			if (ns.DocInfo != null)
			{
				body.Add(x("p", XMLUtils.GetInnerXml(ns.DocInfo,Navigation)));
			}
			for (var kind = TypeDom.TypeKindEnum.Class; kind <= TypeDom.TypeKindEnum.Enum; kind++)
			{
				body.Add(BuildNsSection(
					kind, 
					ns.Types.OrderBy(t => t.SimpleName).Where(_=>_.TypeKind==kind).ToArray()
				));
			}
			return doc.ToString();
		}

		static XElement BuildNsSection(TypeDom.TypeKindEnum kind, TypeDom[] types)
		{
			if (!types.Any())
				return null;

			XElement tbody;

			var ret = Section(
				Names[kind + "_s"],
				x("table", a("class", "doc_table"), tbody = x("tbody"))
				);

			tbody.Add(BuildRow(new XElement[0], x("span", Names[Strings.Name]), Names[Strings.Description].ToSpan(), "th"));

			foreach (var type in types)
				tbody.Add(BuildRow(
					Enumerable.Repeat(GetImage(Navigation.GetIconCss((kind + (type.Type.IsPublic ? "Pub" : "Prot")).To<MemberIconsEnum>()), Names[kind.ToString()], Navigation.EmptyImage, Names[kind.ToString()]), 1), 
					BuildTypeUrl(type.Type, false),
					XMLUtils.GetTagInnerXml(type.DocInfo, "summary", Navigation, false)
					));
			return ret;
		}
	}
}
