#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: XMLCommToHTM.DocViewer
File: GenerateHtml_Type.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using XMLCommToHTM.DOM;

namespace XMLCommToHTM
{
	using System.Runtime.CompilerServices;

	using Ecng.Common;
	using Ecng.Reflection;

	//Type page
	partial class GenerateHtml
	{
		static string GeneratePartialType(TypePartialData partialType)
		{
			var doc = GetDoc(out var body);
			body.Add(Section(
				partialType.Type.SimpleName + Names[Strings.SuffixDelimeter] + Names[partialType.SectionType.ToString()]),
				BuildMembers(partialType.SectionType, partialType.Type)
				);
			return doc.ToString();
		}

		static string Generate(TypeDom type)
		{
			var doc = GetDoc(out var body);

			body.Add(
				x("h1", type.GetDisplayName(false) + Names[Strings.SuffixDelimeter] + Names[type.TypeKind.ToString()]),
				x("p", XMLUtils.GetTagInnerXml(type.DocInfo, "summary", Navigation, true)),
				InheritanceSection(type),
				NamespaceAssembly(type.Type),
				GenerateSyntax(type),
				BuldParameters(Names[Strings.TypeParameters], type.GenericTypeParameters)
			);
			
			for (int i = 0; i <= (int)MemberTypeSection.Events; i++)
			{
				body.Add(BuildMembers((MemberTypeSection)i, type));
			}

			body.Add(BuildEnding(type.Assembly, type.DocInfo));

			return doc.ToString();
		}

		private static XElement GenerateSyntax(TypeDom type)
		{
			var parts = new List<XElement>();

			foreach (var attr in type.Type.GetAttributes(false).Where(a1 => a1.GetType() != typeof(ExtensionAttribute)))
			{
				parts.Add(BuildTypeUrl(attr.GetType(), false, true));
				parts.Add(x("br"));
			}

			parts.Add(x("span", type.Type.IsPublic ? "public" : "protected", a("style", "color:Blue;")));

			if (type.Type.IsStatic())
				parts.Add(x("span", "static", a("style", "color:Blue;")));

			parts.Add(x("span", type.TypeKind.ToString().ToLowerInvariant(), a("style", "color:Blue;")));
			parts.Add(x("span", type.Name));

			var interfaces = type.Type.GetInterfaces();

			if (type.TypeKind == TypeDom.TypeKindEnum.Class)
			{
				if (type.Type.BaseType != typeof(object) || interfaces.Length > 0)
				{
					parts.Add(x("span", " : "));

					if (type.Type.BaseType != typeof(object))
					{
						parts.Add(BuildTypeUrl(type.Type.BaseType, false));

						if (interfaces.Length > 0)
							parts.Add(x("span", ","));
					}

					foreach (var itf in interfaces)
					{
						parts.Add(BuildTypeUrl(itf, false));
						parts.Add(x("span", ","));
					}

					if (interfaces.Length > 0)
						parts.RemoveAt(parts.Count - 1);
				}
			}
			else if (type.TypeKind == TypeDom.TypeKindEnum.Enum)
			{
				if (type.Type.GetEnumBaseType() != typeof(int))
				{
					parts.Add(x("span", " : "));
					parts.Add(BuildTypeUrl(type.Type.BaseType, false));
				}
			}
			else if (type.TypeKind == TypeDom.TypeKindEnum.Interface || type.TypeKind == TypeDom.TypeKindEnum.Struct)
			{
				if (interfaces.Length > 0)
				{
					parts.Add(x("span", " : "));

					foreach (var itf in interfaces)
					{
						parts.Add(BuildTypeUrl(itf, false));
						parts.Add(x("span", ","));
					}

					if (interfaces.Length > 0)
						parts.RemoveAt(parts.Count - 1);
				}
			}

			return x("div", a("class", "doc_syntax"), x("code", parts));
		}
		
		static XElement NamespaceAssembly(Type type)
		{
			return
				x("p",
					a("class", "doc_assembly"),
					x("b", Names[Strings.Namespace] + ": "), BuildNsUrl(type.Namespace),
					x("br"),
					x("b", 
						Names[Strings.Assembly] + ": "), type.Assembly.GetName().Name +
						" (" + Path.GetFileName(type.Assembly.Location) + ") " + Names[Strings.Version] + " " + type.Assembly.GetName().Version
				);
		}


		static XElement InheritanceSection(TypeDom type)
		{
			if (type.Type.IsValueType || type.Type.IsEnum || type.Type.IsInterface || type.Type.IsSubclassOf(typeof(Delegate)))
				return x("div");

			var space = string.Empty;
			var content = new XElement("div");
			var baseTypes = type.BaseTypes;
			var derivedTypes = type.DerivedTypes;

			if (baseTypes.Length == 0 && derivedTypes.Length == 0)
				return null;

			foreach (var baseType in baseTypes)
			{
				content.Add(space, BuildTypeUrl(baseType), x("br"));
				space += Nbsp + Nbsp;
			}

			content.Add(x("b", space + type.GetDisplayName(true)), x("br"));
			space += Nbsp + Nbsp;

			foreach (var derivedType in derivedTypes)
				content.Add(space, BuildTypeUrl(derivedType), x("br"));

			return Section(Names[Strings.InheritanceHierarchy], content, a("class", "doc_inheritance"));
		}

		static XElement BuildMembers(MemberTypeSection section, TypeDom type)
		{
			switch (section)
			{
				case MemberTypeSection.NestedTypes:
					return BuildNestedTypes(type.NestedTypes);
				case MemberTypeSection.Constructors:
					return BuildMembers(Strings.Constructors, type.Constructors);
				case MemberTypeSection.Properties:
					return BuildMembers(Strings.Properties, type.Properties);
				case MemberTypeSection.Methods:
					return BuildMembers(Strings.Methods, type.Methods);
				case MemberTypeSection.ExtentionMethods:
					return BuildMembers(Strings.ExtentionMethods, type.ExtentionMethods, true);
				case MemberTypeSection.Operators:
					return BuildMembers(Strings.Operators, type.Operators);
				case MemberTypeSection.Fields:
					return BuildMembers(Strings.Fields, type.Fields);
				case MemberTypeSection.Events:
					return BuildMembers(Strings.Events, type.Events);
				default: throw new Exception();
			}
		}

		struct MemberColumns
		{
			public IEnumerable<XElement> Icons;
			public XElement Name;
			public XElement Description;
		}

		static XElement BuildNestedTypes(IEnumerable<TypeDom> members)
		{
			if (members == null || !members.Any())
				return null;
			return BuildMembers(Names[Strings.NestedTypes], members.Select(_ =>
				new MemberColumns
				{
					Name =  BuildTypeUrl(_.Type),
					Description = XMLUtils.GetTagInnerXml(_.DocInfo, "summary", Navigation, false)
				}
			));
		}

		static XElement BuildMembers(string name, IEnumerable<MemberDom> members, bool asExtention = false)
		{
			return BuildMembers(Names[name], members.Select(_ =>
				new MemberColumns
				{
					Icons = GetMemberIcons(_),
					Name = BuildMemberUrl(_, asExtention),
					Description = GetMemberDescription(_)
				}
			));
		}

		private static IEnumerable<XElement> GetMemberIcons(MemberDom member)
		{
			MemberIconsEnum? memberType = null;
			var memberName = "";
			if (member is ConstructorDom)
			{
				memberType = member.IsPublic ? MemberIconsEnum.MethodPub : MemberIconsEnum.MethodProt;
				memberName = "Method";
			}
			if (member is MethodDom method)
			{
				if (method.IsOperator)
				{
					memberType = member.IsPublic ? MemberIconsEnum.OperatorPub : MemberIconsEnum.OperatorProt;
					memberName = "Operator";
				}
				else if (method.IsExtention)
				{
					if (!member.IsPublic)
						throw new InvalidOperationException();

					memberType = MemberIconsEnum.MethodExtPub;
					memberName = "Extention";
				}
				else
				{
					memberType = member.IsPublic ? MemberIconsEnum.MethodPub : MemberIconsEnum.MethodProt;
					memberName = "Method";
				}
			}
			else if (member is PropertyDom)
			{
				memberType = member.IsPublic ? MemberIconsEnum.PropertyPub : MemberIconsEnum.PropertyProt;
				memberName = "Property";
			}
			else if (member is FieldDom)
			{
				memberType = member.IsPublic ? MemberIconsEnum.FieldPub : MemberIconsEnum.FieldProt;
				memberName = "Field";
			}
			else if (member is EventDom)
			{
				memberType = member.IsPublic ? MemberIconsEnum.EventPub : MemberIconsEnum.EventProt;
				memberName = "Event";
			}

			memberName += member.IsPublic ? "Pub" : "Prot";

			if (memberType != null)
				yield return GetImage(Navigation.GetIconCss(memberType.Value), Names[memberName], Navigation.EmptyImage, Names[memberName]);
			if(member.IsStatic)
				yield return GetImage(Navigation.GetIconCss(MemberIconsEnum.Static), Names[Strings.StaticMember], Navigation.EmptyImage, Names[Strings.StaticMember]);
		}

		static XElement GetMemberDescription(MemberDom m)
		{
			var ret = XMLUtils.GetTagInnerXml(m.DocInfo, "summary", Navigation, false) ?? string.Empty.ToSpan();

			var inherited = m.GetInheritedFrom();
			var overrides = m.GetOverrides();

			if (overrides != null)
				ret.Add((" (" + Names[Strings.Overrides] + " ").ToSpan(), BuildMemberUrl(overrides, false, true), ")".ToSpan());
			else if (inherited != null)
				ret.Add((" (" + Names[Strings.InheritedFrom] + " ").ToSpan(), BuildTypeUrl(inherited), ")".ToSpan());

			return ret;
		}

		static string GetShortSignature(MemberDom m, bool asExtention, bool showType = false)
		{
			if (!asExtention)
				return showType ? m.Type.Type.Name + "." + m.ShortSignature : m.ShortSignature;

			if (m is MethodDom meth && meth.IsExtention)
				return meth.GetShortSignature(true);
			else
				return m.ShortSignature;
		}
		static XElement BuildMemberUrl(MemberDom member, bool asExtention, bool showType = false)
		{
			var ret = x("a", GetShortSignature(member, asExtention, showType));
			string href = Navigation.GetMemberHref(member);
			if (!string.IsNullOrEmpty(href))
				ret.Add(a("href", href));
			return ret;
		}


		static XElement BuildMembers(string header, IEnumerable<MemberColumns> members)
		{
			if (!members.Any())
				return null;

			XElement tbody;

			var ret = Section(header,
				x("table", a("class", "doc_table"), tbody = x("tbody"))
			);
			
			tbody.Add(BuildRow(new XElement[0], x("span", Names[Strings.Name]), Names[Strings.Description].ToSpan(), "th"));
			
			foreach (var member in members)
				tbody.Add(BuildRow(member.Icons, member.Name, member.Description));

			return ret;
		}

		//static XElement BuildRow(params string[] sAr)
		//{
		//    var ret=new x("tr");
		//    for (int i = 0; i < sAr.Length; i++)
		//    {
		//        ret.Add( xx("td", sAr[i]));
		//    }
		//    return ret;
		//}

	}
}
