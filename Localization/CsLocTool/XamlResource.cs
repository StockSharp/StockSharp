using System;
using System.Collections.Generic;
using System.Linq;
using Ecng.Common;

namespace CsLocTool {
	class XamlResource {
		private const string DefaultAttrName = "_DefaultText";

		public string ElementName {get; private set;}
		public string AttrName {get; private set;}
		public string Text {get; private set;}
		public string TextTrim {get; private set;}
		public string Filename {get; set;}
		public bool IsDefaultText {get; private set;}

		public XamlResource(string elementName, string attrName, string text)
		{
			ElementName = elementName;
			if (attrName.IsEmpty())
			{
				AttrName = DefaultAttrName;
				IsDefaultText = true;
			}
			else
			{
				AttrName = attrName;
			}
			Text = text;
			TextTrim = text.Trim();
		}

		public string GetReplacementText(IEnumerable<StringResource> strResources, out bool trim)
		{
			trim = false;
			var res = strResources.FirstOrDefault(r => r.RusString == Text);
			if (res == null)
			{
				res = strResources.FirstOrDefault(r => r.RusString == TextTrim);
				if(res == null)
					return null;

				trim = true;
			}

			if (AttrName != DefaultAttrName)
			{
				if(trim && Char.IsWhiteSpace(Text[0]))
					return null;

				trim = false;

				return Text.Replace(TextTrim, "{x:Static loc:LocalizedStrings." + res.ConstantName + "}");
			}

			trim = false;

			if (Text == TextTrim)
				return "<TextBlock Text=\"{x:Static loc:LocalizedStrings." + res.ConstantName + "}\"/>";

			var rep = Text.Replace(TextTrim, "<Run Text=\"{x:Static loc:LocalizedStrings." + res.ConstantName + "}\"/>");

			var firstBrace = rep.IndexOf('<');
			var lastBrace = rep.LastIndexOf('>');

			if(firstBrace < 0 || lastBrace < 0)
				throw new InvalidOperationException("'<' or '>' not found");

			var start = rep.Substring(0, firstBrace);
			var end = rep.Substring(lastBrace);
			var middle = rep.Substring(firstBrace, lastBrace - firstBrace + 1);

			return "<TextBlock>" + 
				(start.IsEmpty() ? string.Empty : "<Run Text=\""+ start +"\"/>") +
				middle +
				(end.IsEmpty() ? string.Empty : "<Run Text=\""+ end +"\"/>") +
				"</TextBlock>";
		}
	}
}
