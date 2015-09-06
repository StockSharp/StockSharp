using System.Reflection;
using System.Xml.Linq;

namespace XMLCommToHTM.DOM
{
	public class FieldDom : MemberDom
	{
		private FieldInfo _fi;
		public FieldDom(FieldInfo fi, XElement doc): base(fi, doc)
		{
			_fi = fi;
		}
		public override bool IsPublic { get { return _fi.IsPublic; } }
		public override bool IsPrivateOrInternal { get { return _fi.IsPrivate || _fi.IsAssembly; } }
		public override bool IsStatic { get { return _fi.IsStatic; } }

		public override System.Type MemberType
		{
			get { return _fi.FieldType; }
		}
	}
}