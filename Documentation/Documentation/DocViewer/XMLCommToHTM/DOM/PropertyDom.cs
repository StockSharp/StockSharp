#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: XMLCommToHTM.DOM.DocViewer
File: PropertyDom.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
using System;
using System.Reflection;
using System.Xml.Linq;
using XMLCommToHTM.DOM.Internal;

namespace XMLCommToHTM.DOM
{
	using System.Linq;

	//Для property с параметрами есть тег returns
	public class PropertyDom: MemberDom
	{
		private readonly PropertyInfo _pi;
		public PropertyDom(PropertyInfo pi, XElement doc) : base(pi, doc)
		{
			_pi = pi;
			Params = ParameterDom.BuildParameters(pi.GetIndexParameters(), doc);
		}
		public override string ShortSignature => base.ShortSignature + GetParametersShortSignature();

		public override string GetParametersShortSignature()
		{
			return MemberUtils.GetParametersShortSignature(_pi.GetIndexParameters());
		}
		public override bool IsPublic => (_pi.GetMethod != null && _pi.GetMethod.IsPublic) || (_pi.SetMethod!=null && _pi.SetMethod.IsPublic);

		public override bool IsPrivateOrInternal => (_pi.GetMethod == null || _pi.GetMethod.IsPrivate || _pi.GetMethod.IsAssembly) &&
		                                            (_pi.SetMethod == null || _pi.SetMethod.IsPrivate || _pi.SetMethod.IsAssembly);

		public override bool IsStatic => (_pi.GetMethod != null && _pi.GetMethod.IsStatic) || (_pi.SetMethod != null && _pi.SetMethod.IsStatic);

		public override Type MemberType => _pi.PropertyType;

		public override MemberDom GetOverrides()
		{
			var mi = _pi.GetAccessors().FirstOrDefault(a => a.IsVirtual);

			if (mi == null)
				return null;

			var baseType = mi.GetBaseDefinition().DeclaringType;

			if (mi.DeclaringType == baseType || mi.DeclaringType != Type.Type || baseType == null)
				return null;

			return new PropertyDom(baseType.GetProperty(_pi.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic), new XElement(_pi.Name))
			{
				Type = new TypeDom { Type = baseType }
			};
		}
	}
}