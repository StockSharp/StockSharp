using System;
using System.Reflection;
using System.Xml.Linq;

namespace XMLCommToHTM.DOM
{
	public abstract class MemberDom
	{
		public ParameterDom[] Params;
		protected MemberDom(MemberInfo memberInfo, XElement doc)
		{
			DocInfo = doc;
			_memberInfo = memberInfo;
		}
		public XElement DocInfo;
		readonly MemberInfo _memberInfo;
		public TypeDom Type;

		public static MemberDom Build(TypeDom typeDom, MemberInfo memberInfo, XElement doc)
		{
			MemberDom ret;
			if (memberInfo is MethodInfo)
				ret = new MethodDom(memberInfo as MethodInfo, doc);
			else if (memberInfo is ConstructorInfo)
				ret = new ConstructorDom(memberInfo as ConstructorInfo, doc);
			else if (memberInfo is PropertyInfo)
				ret = new PropertyDom(memberInfo as PropertyInfo, doc);
			else if (memberInfo is EventInfo)
				ret = new EventDom(memberInfo as EventInfo, doc);
			else if (memberInfo is FieldInfo)
				ret = new FieldDom(memberInfo as FieldInfo, doc);
			else
				throw new Exception();
			ret.Type = typeDom; //ToTo: передавать конструктор? Иначе в конструкторе _typeDom==null
			return ret;
		}
		//public XElement DocInfo2 { get { return _docInfo; } }

		public virtual string Name
		{
			get { return _memberInfo.Name; }
		}
		public virtual string ShortSignature
		{
			get { return _memberInfo.Name; }
		}
		public virtual string SimpleName
		{
			get
			{
				if (_memberInfo.Name.Contains("`"))
					return _memberInfo.Name.Split('`')[0];
				else
					return _memberInfo.Name;
			}
		}
		public override string ToString()
		{
			return _memberInfo.ToString();
		}
		public Type GetInheritedFrom()
		{
			if (_memberInfo.DeclaringType == Type.Type || _memberInfo.DeclaringType==null)
				return null; //not inherited
			return _memberInfo.DeclaringType;
		}
		public Type DeclaringType { get { return _memberInfo.DeclaringType; } }

		public virtual MemberDom GetOverrides()
		{
			return null;
		}

		public virtual string GetParametersShortSignature()
		{
			return null;
		}

		public abstract bool IsPublic { get; }
		public abstract bool IsPrivateOrInternal { get; }
		public abstract bool IsStatic { get; }

		public abstract Type MemberType { get; }

		public virtual GenericParameterDom[] GenericArguments { get { return null; } }

		public virtual XElement GetParametersLongSignature()
		{
			return new XElement("span");
		}
	}

}
