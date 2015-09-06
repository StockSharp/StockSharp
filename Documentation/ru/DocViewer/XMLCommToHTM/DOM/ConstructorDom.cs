using System.Reflection;
using System.Xml.Linq;
using XMLCommToHTM.DOM.Internal;

namespace XMLCommToHTM.DOM
{
	public class ConstructorDom : MemberDom
	{
		private ConstructorInfo _ci;
		public ConstructorDom(ConstructorInfo ci, XElement doc) : base(ci, doc)
		{
			_ci = ci;
			Params = ParameterDom.BuildParameters(ci.GetParameters(), doc);
		}
		public override string ShortSignature
		{
			get
			{
				string ret= TypeUtils.SimpleName(_ci.DeclaringType);
				ret += GetParametersShortSignature();
				return ret;
			}
		}

		public override string GetParametersShortSignature()
		{
			return MemberUtils.GetParametersShortSignature(_ci.GetParameters());
		}
		public override bool IsPublic { get { return _ci.IsPublic; } }
		public override bool IsPrivateOrInternal { get { return _ci.IsPrivate || _ci.IsAssembly; } }
		public override bool IsStatic { get { return _ci.IsStatic; } }

		public override System.Type MemberType
		{
			get { return null; }
		}
	}
}