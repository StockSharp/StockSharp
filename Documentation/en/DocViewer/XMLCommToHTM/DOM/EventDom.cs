using System.Reflection;
using System.Xml.Linq;

namespace XMLCommToHTM.DOM
{
	public class EventDom : MemberDom
	{
		private EventInfo _ei;
		public EventDom(EventInfo ei, XElement doc): base(ei, doc)
		{
			_ei = ei;
		}
		public override bool IsPublic
		{
			get
			{
				return ( _ei.AddMethod!=null && _ei.AddMethod.IsPublic) || (_ei.RemoveMethod!=null && _ei.RemoveMethod.IsPublic);
			}
		}
		public override bool IsPrivateOrInternal
		{
			get
			{
				
				return	(_ei.AddMethod == null || _ei.AddMethod.IsPrivate || _ei.AddMethod.IsAssembly) &&
						(_ei.RemoveMethod == null || _ei.RemoveMethod.IsPrivate || _ei.RemoveMethod.IsAssembly);
			}
		}

		public override bool IsStatic
		{
			get
			{
				return (_ei.AddMethod!=null &&_ei.AddMethod.IsStatic) || (_ei.RemoveMethod!=null && _ei.RemoveMethod.IsStatic);
			}
		}
		public override System.Type MemberType
		{
			get { return null; }
		}
	}
}