#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: XMLCommToHTM.DOM.Internal.DocViewer
File: NamespaceComments.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace XMLCommToHTM.DOM.Internal
{
	using System.Collections.Generic;

	public class NamespaceComments
	{
		public string NamespaceName;
		public XElement Comments;
	}

	public static class NamespaceCommentsParser
	{
		public static IDictionary<string, NamespaceComments> Parse(XElement doc)
		{
			var res = doc
				.XPathSelectElement("members")
				.XPathSelectElements("member[starts-with(@name,\"N:\")]")
				.ToArray();

			return res
				.Select(_ =>
					new NamespaceComments
						{
							NamespaceName = _.Attribute("name").Value.Substring(2), 
							Comments = _
						}
					)
				.ToDictionary(_ => _.NamespaceName, _ => _);
		}
	}
}
