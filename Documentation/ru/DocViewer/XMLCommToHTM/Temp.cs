using XMLCommToHTM.DOM;

namespace XMLCommToHTM
{
	public enum MemberTypeSection { NestedTypes = 0, Constructors, Properties, Methods, ExtentionMethods, Operators, Fields, Events } 
	public class TypePartialData
	{
		public TypePartialData() { }
		public TypePartialData(TypeDom type, MemberTypeSection sectionType)
		{
			Type = type;
			SectionType = sectionType;
		}
		public TypeDom Type;
		public MemberTypeSection SectionType;
	}
}
