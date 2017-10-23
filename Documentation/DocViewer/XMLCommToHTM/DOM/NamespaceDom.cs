#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: XMLCommToHTM.DOM.DocViewer
File: NamespaceDom.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
using System.Xml.Linq;

namespace XMLCommToHTM.DOM
{
	using System.Collections.Generic;

	public class NamespaceDom
	{
		public List<TypeDom> Types = new List<TypeDom>();
		public XElement DocInfo;

		public NamespaceDom(string name)
		{
			Name = name;
		}

		public string Name { get; }
	}
}
