#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: XMLCommToHTM.DocViewer
File: XMLUtils.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
using System.Linq;
using System.Xml.Linq;

namespace XMLCommToHTM
{
	using Ecng.Common;

	public static class XMLUtils
	{
		private static Navigation _nav;

		public static XElement GetTagInnerXml(XElement docInfo, string tag, Navigation nav, bool addCss)
		{
			_nav = nav;

			if (docInfo == null)
				return null;

			ReplaceSee(docInfo);
			var s = docInfo.Elements(tag).Select(e => GetInnerXmlPriv(e, addCss)).ToArray();

			if (s.Length == 0)
				return null;

			return new XElement("span", s);
		}
		//public static string GetTagText2(XElement docInfo, string tag)
		//{
		//	return GetTagText(docInfo, tag, _=>_.Value);
		//}

		public static XElement ToSpan(this string content)
		{
			return new XElement("span", content);
		}

		public static XElement GetInnerXml(XElement elem, Navigation nav)
		{
			_nav = nav;
			return GetInnerXmlPriv(elem, false);
		}

		static XElement GetInnerXmlPriv(XElement elem, bool addCss)
		{
			if (elem == null)
				return null;

			ReplaceSee(elem);

			using (var r = elem.CreateReader(ReaderOptions.OmitDuplicateNamespaces))
			{
				r.MoveToContent();
				var str = r.ReadInnerXml().Trim();

				//if (str.FirstOrDefault() != '<')
				str = "<span>{0}</span>".Put(str);

				var retVal = new XElement("div", XElement.Parse(str));

				if (addCss)
					retVal.Add(new XAttribute("class", "doc_" + elem.Name));

				return retVal;
			}
		}

		/// <summary>
		/// Создает элемент, и добавляет содержимое innerXML как XML содержимое, а не как строку 
		/// Т.е. при создании не делает escape для строки xmlPart.
		/// Другого способа не нашлось
		/// </summary>
		/// <param name="tagName"></param>
		/// <param name="innerXml"></param>
		/// <returns></returns>
		public static XElement NewXElement(string tagName, string innerXml)
		{
			string s2 = string.Format("<{0}>{1}</{0}>",tagName,innerXml);
			return XElement.Parse(s2, LoadOptions.PreserveWhitespace);
		}

		static void ReplaceSee(XElement elem)
		{
			var see=elem.Descendants("see").ToArray();

			for (int i = 0; i < see.Length; i++)
			{
				if (see[i].Attribute("cref") == null)
				{
					see[i].AddAfterSelf("");
				}
				else
				{
					var s = see[i].Attribute("cref").Value.Substring(2);
					var link = GenerateHtml.BuildUrl(s, _nav.GetSeeTagHref(s));
					see[i].AddAfterSelf(link);
				}
				see[i].Remove();	
			}
			
		}
		//System.Security.SecurityElement.Escape(xml);

	}
}
