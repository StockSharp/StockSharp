#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: XMLCommToHTM.DocViewer
File: GenerateHtml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using XMLCommToHTM.DOM;
using XMLCommToHTM.DOM.Internal;

namespace XMLCommToHTM
{
	using Ecng.Common;

	public static partial class GenerateHtml
	{
		public static Navigation Navigation;
		
		static XElement x(XName name, params object[] content) { return new XElement(name, content); }
		static XElement x(XName name, object content) { return new XElement(name, content); }
		static XElement xx(string tagNeme, string innerXml) { return XMLUtils.NewXElement(tagNeme, innerXml); }
		static XAttribute a(XName name, object content) { return new XAttribute(name, content); }
		
		private static readonly Strings Names=new Strings();

		public static bool IsRussian
		{
			get => Names.IsRussian;
			set => Names.IsRussian = value;
		}

		public static string CssUrl { get; set; }

		public static bool IsHtmlAsDiv { get; set; }

		public static string NestedTypesName => Names[Strings.NestedTypes];

		public static string GetSectionName(MemberTypeSection section)
		{
			return Names[section.ToString()];
		}

		public static string Generate(object pageData)
		{
			switch (pageData)
			{
				case TypeDom td:
					return Generate(td);
				case TypePartialData tpd:
					return GeneratePartialType(tpd);
				case MemberDom md:
					return Generate(md);
				case NamespaceDom nd:
					return Generate(nd);
				case IGrouping<string,MethodDom> g:
					return Generate(g);
				case SolutionDom sd:
					return Generate(sd);
				default:
					throw new NotSupportedException();
			}
		}

		static readonly string Nbsp = "" + (char)0x00a0;

		static XDocument GetDoc(out XElement body)
		{
			if (IsHtmlAsDiv)
			{
				body = x("div");
				return new XDocument(body);
			}

			body = x("body");

			var doc = new XDocument(
				new XDocumentType("html", null, null, null),
				//XNamespace ns = "http://www.w3.org/1999/xhtml";
				x("html",
					x("head",
					   x("meta", a("http-equiv", "Content-Type"),
						 a("content", "text/html;charset=UTF-8")),
					   x("link",
							a("href", CssUrl),
							a("rel", "stylesheet"),
							a("type", "text/css"))
					   ),
					body
				)
			);

			return doc;
		}
		public static XElement Section(string header, params object[] content)
		{
			return
				x("div",
					x("h2", header),
					content
					);
		}

		static XElement BuildEnding(AssemblyDom asm,XElement docInfo)
		{
			var remarks = XMLUtils.GetTagInnerXml(docInfo, "remarks", Navigation, true);

			return 
				x("div",
					remarks == null ? x("span") : Section(Names[Strings.Remarks], x("p", remarks)),
					Section(Names[Strings.VersionInfo],
						x("p",
							x("b", Names[Strings.Version] + ": "), asm.Version,
							x("br"),
							x("b", Names[Strings.NetRuntimeVersion] + ": "), asm.RuntimeVersion
						),
						a("class", "doc_version")
					)
				);

		}

		static XElement BuildRow(IEnumerable<XElement> images, XElement name, XElement description, string td = "td")
		{
			return x("tr",
				x(td, images),
				x(td, name),
				x(td, description)
			);
		}
		static XElement GetImage(string css, string alt, string clearUrl, string title)
		{
			return x("img",
				a("src", clearUrl),
				a("alt", alt),
				a("title", title),
				a("class", css)
			);
		}

		public static XElement BuildTypeUrl(Type type, bool includeNamespace = true, bool truncateIfAfftibute = false)
		{
			var name = TypeUtils.ToDisplayString(type, includeNamespace);

			if (truncateIfAfftibute && type.IsSubclassOf(typeof(Attribute)))
				name = "[" + name.Remove("Attribute") + "]";

			return BuildUrl(name, Navigation.GetTypeHref(type));
		}

		public static XElement BuildNsUrl(string ns)
		{
			return BuildUrl(ns, Tuple.Create(Navigation.UrlPrefix + ns.Replace('.', '/'), true));
		}

		public static XElement BuildUrl(string name, Tuple<string, bool> href)
		{
			var ret = x("a", name);

			if (href != null && !href.Item1.IsEmpty())
			{
				ret.Add(a("href", href.Item1));

				if (!href.Item2)
					ret.Add(a("target", "_blank"));
			}

			return ret;
		}

		static XElement BuldParameters(string header, ParameterBaseDom[] Params)
		{
			if (Params == null || Params.Length == 0)
				return null;
			var ret =
				x("div",
				  x("h4", header, a("class", "doc_h4")),
				  Params.Select(BuildParameter).ToArray()
				);
			return ret;
		}
		static XElement BuildParameter(ParameterBaseDom p)
		{
			XElement dd;
			var ret =
				x("dl",
					x("dt", p.Name),
					dd = x("dd")
				);

			if (p.Type != null)
				dd.Add(Names[Strings.Type] + ": ", BuildTypeUrl(p.Type), x("br"));

			dd.Add(XMLUtils.GetInnerXml(p.DocInfo, Navigation));
			return ret;
		}
	}
}
