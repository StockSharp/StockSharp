#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: XMLCommToHTM.DocViewer
File: Temp.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
