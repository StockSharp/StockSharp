#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: XMLCommToHTM.DOM.DocViewer
File: EventDom.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
using System.Reflection;
using System.Xml.Linq;

namespace XMLCommToHTM.DOM
{
	public class EventDom : MemberDom
	{
		private readonly EventInfo _ei;
		public EventDom(EventInfo ei, XElement doc): base(ei, doc)
		{
			_ei = ei;
		}
		public override bool IsPublic => ( _ei.AddMethod!=null && _ei.AddMethod.IsPublic) || (_ei.RemoveMethod!=null && _ei.RemoveMethod.IsPublic);

		public override bool IsPrivateOrInternal => (_ei.AddMethod == null || _ei.AddMethod.IsPrivate || _ei.AddMethod.IsAssembly) &&
		                                            (_ei.RemoveMethod == null || _ei.RemoveMethod.IsPrivate || _ei.RemoveMethod.IsAssembly);

		public override bool IsStatic => (_ei.AddMethod!=null &&_ei.AddMethod.IsStatic) || (_ei.RemoveMethod!=null && _ei.RemoveMethod.IsStatic);
		public override System.Type MemberType => null;
	}
}