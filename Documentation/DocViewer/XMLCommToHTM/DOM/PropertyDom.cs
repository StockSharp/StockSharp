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
		public override string ShortSignature
		{
			get
			{
				return base.ShortSignature + GetParametersShortSignature();
			}
		}
		public override string GetParametersShortSignature()
		{
			return MemberUtils.GetParametersShortSignature(_pi.GetIndexParameters());
		}
		public override bool IsPublic
		{
			get
			{
				return (_pi.GetMethod != null && _pi.GetMethod.IsPublic) || (_pi.SetMethod!=null && _pi.SetMethod.IsPublic);
			}
		}
		public override bool IsPrivateOrInternal
		{
			get
			{

				return (_pi.GetMethod == null || _pi.GetMethod.IsPrivate || _pi.GetMethod.IsAssembly) &&
						(_pi.SetMethod == null || _pi.SetMethod.IsPrivate || _pi.SetMethod.IsAssembly);
			}
		}


		public override bool IsStatic
		{
			get
			{
				return (_pi.GetMethod != null && _pi.GetMethod.IsStatic) || (_pi.SetMethod != null && _pi.SetMethod.IsStatic);
			}
		}
		public override Type MemberType
		{
			get { return _pi.PropertyType; }
		}

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