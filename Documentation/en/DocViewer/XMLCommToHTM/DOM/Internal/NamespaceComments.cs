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
