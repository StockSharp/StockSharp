#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: XMLCommToHTM.DOM.DocViewer
File: MethodDom.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using XMLCommToHTM.DOM.Internal;

namespace XMLCommToHTM.DOM
{
	using System.Linq;

	using Ecng.Common;

	public class MethodDom: MemberDom
	{
		private readonly MethodInfo _mi;
		public int? OverloadIndex;

		public MethodDom(MethodInfo mi, XElement doc) : base(mi, doc)
		{
			_mi = mi;
			Params = ParameterDom.BuildParameters(mi.GetParameters(), doc);
			GenericArguments = GenericParameterDom.BuildMethodGenericParameters(_mi, doc);
		}
		public override GenericParameterDom[] GenericArguments { get; }

		public override string Name
		{
			get
			{
				if (IsOperator)
					return base.Name.Remove("op_");
				else 
					return base.Name;
			}
		}

		public bool IsOperator => _mi.IsSpecialName && _mi.Name.StartsWith("op_");

		public override string ShortSignature => GetShortSignature(false);

		public string GetShortSignature(bool asExtention)
		{
			var ret = base.ShortSignature;
			if (IsOperator)
				ret = ret.Remove("op_");
			ret += MemberUtils.GetGenericListSignature(_mi);
			ret += MemberUtils.GetParametersShortSignature(_mi.GetParameters(), (asExtention?1:0));
			return ret;
		}
		public override string GetParametersShortSignature()
		{
			return MemberUtils.GetParametersShortSignature(_mi.GetParameters());
		}
		public override XElement GetParametersLongSignature()
		{
			return MemberUtils.GetParametersLongSignature(_mi.GetParameters());
		}
		public bool IsExtention => _mi.IsStatic && _mi.IsDefined(typeof (ExtensionAttribute), false) && _mi.GetParameters().Length > 0;
		public override bool IsPublic => _mi.IsPublic;
		public override bool IsPrivateOrInternal => _mi.IsPrivate || _mi.IsAssembly;
		public override bool IsStatic => _mi.IsStatic;

		public Type FirtParameterType => _mi.GetParameters()[0].ParameterType;

		public override MemberDom GetOverrides()
		{
			if (!_mi.IsVirtual)
				return null;

			var baseType = _mi.GetBaseDefinition().DeclaringType;

			if (_mi.DeclaringType == baseType || _mi.DeclaringType != Type.Type || baseType == null)
				return null;

			var types = _mi.GetParameters().Select(pi => pi.ParameterType).ToArray();

			var mi = (_mi.IsGenericMethod)
				? baseType.GetGenericMethod(_mi.Name, types)
				: baseType.GetMethod(_mi.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, types, new ParameterModifier[0]);

			return new MethodDom(mi, new XElement(_mi.Name))
			{
				Type = new TypeDom { Type = baseType }
			};
		}

		public override Type MemberType => _mi.ReturnType;
	}
}