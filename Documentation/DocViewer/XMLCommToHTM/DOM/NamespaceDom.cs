#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: XMLCommToHTM.DOM.DocViewer
File: NamespaceDom.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
using System.Xml.Linq;

namespace XMLCommToHTM.DOM
{
	using Wintellect.PowerCollections;

	public class NamespaceDom
	{
		public OrderedBag<TypeDom> Types = new OrderedBag<TypeDom>((t1, t2) => t1.SimpleName.CompareTo(t2.SimpleName));
		public XElement DocInfo;

		private readonly string _name;

		public NamespaceDom(string name)
		{
			_name = name;
		}

		public string Name
		{
			get { return _name; }
		}
	}
}
