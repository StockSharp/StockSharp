using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using XMLCommToHTM.DOM.Internal;

namespace XMLCommToHTM.DOM
{
	using System.Linq;

	public class MethodDom: MemberDom
	{
		private readonly MethodInfo _mi;
		readonly GenericParameterDom[] _genericArguments;
		public int? OverloadIndex;

		public MethodDom(MethodInfo mi, XElement doc) : base(mi, doc)
		{
			_mi = mi;
			Params = ParameterDom.BuildParameters(mi.GetParameters(), doc);
			_genericArguments = GenericParameterDom.BuildMethodGenericParameters(_mi, doc);
		}
		public override GenericParameterDom[] GenericArguments { get { return _genericArguments; } } 
		public override string Name
		{
			get
			{
				if (IsOperator)
					return base.Name.Replace("op_", "");
				else 
					return base.Name;
			}
		}

		public bool IsOperator
		{
			get { return _mi.IsSpecialName && _mi.Name.StartsWith("op_"); }
		}
		public override string ShortSignature
		{
			get { return GetShortSignature(false); }
		}
		public string GetShortSignature(bool asExtention)
		{
			var ret = base.ShortSignature;
			if (IsOperator)
				ret = ret.Replace("op_","");
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
		public bool IsExtention
		{
			get { return _mi.IsStatic && _mi.IsDefined(typeof (ExtensionAttribute), false) && _mi.GetParameters().Length > 0; }
		}
		public override bool IsPublic { get { return _mi.IsPublic; } }
		public override bool IsPrivateOrInternal { get { return _mi.IsPrivate || _mi.IsAssembly; } }
		public override bool IsStatic { get { return _mi.IsStatic; } }
		public Type FirtParameterType
		{
			get { return _mi.GetParameters()[0].ParameterType; }
		}

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

		public override Type MemberType
		{
			get { return _mi.ReturnType; }
		}
		
	}
}