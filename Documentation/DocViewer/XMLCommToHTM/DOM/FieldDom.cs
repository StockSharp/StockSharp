#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: XMLCommToHTM.DOM.DocViewer
File: FieldDom.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
using System.Reflection;
using System.Xml.Linq;

namespace XMLCommToHTM.DOM
{
	public class FieldDom : MemberDom
	{
		private readonly FieldInfo _fi;
		public FieldDom(FieldInfo fi, XElement doc): base(fi, doc)
		{
			_fi = fi;
		}
		public override bool IsPublic => _fi.IsPublic;
		public override bool IsPrivateOrInternal => _fi.IsPrivate || _fi.IsAssembly;
		public override bool IsStatic => _fi.IsStatic;

		public override System.Type MemberType => _fi.FieldType;
	}
}