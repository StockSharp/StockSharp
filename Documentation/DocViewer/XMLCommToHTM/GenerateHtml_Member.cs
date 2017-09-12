#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: XMLCommToHTM.DocViewer
File: GenerateHtml_Member.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
using System;
using System.Linq;
using System.Xml.Linq;
using XMLCommToHTM.DOM;

namespace XMLCommToHTM
{
	using System.Collections.Generic;

	//Member page
	partial class GenerateHtml
	{
		static string Generate(MemberDom member)
		{
			var doc = GetDoc(out var body);
			body.Add(
				x("h1", member.Type.SimpleName + GetMemberName(member) + Names[Strings.SuffixDelimeter] + 
					Names[GetMethodKindName(member)]+ " ", member.GetParametersLongSignature()),
				x("p", XMLUtils.GetTagInnerXml(member.DocInfo, "summary", Navigation, true)),
				NamespaceAssembly(member.DeclaringType),
				GenerateSyntax(member),
				BuldParameters(Names[Strings.TypeParameters], member.GenericArguments),
				BuldParameters(Names[Strings.Parameters], member.Params),
				BuildReturns(member),
				BuildEnding(member.Type.Assembly, member.DocInfo)
			);
			return doc.ToString();
		}

		private static XElement GenerateSyntax(MemberDom member)
		{
			var parts = new List<XElement> { x("span", member.IsPublic ? "public" : "protected", a("style", "color:Blue;")) };

			if (member.IsStatic)
				parts.Add(x("span", "static", a("style", "color:Blue;")));

			if (member.MemberType == typeof(void))
				parts.Add(x("span", "void", a("style", "color:Blue;")));

			parts.Add(x("span", member.Name));

			if (member.Params != null && member.Params.Length > 0)
			{
				parts.Add(x("span", "("));

				foreach (var param in member.Params)
				{
					parts.Add(BuildTypeUrl(param.Type, false));

					parts.Add(x("span", " " + param.Name));
					parts.Add(x("span", ","));
				}

				parts.RemoveAt(parts.Count - 1);

				parts.Add(x("span", ")"));
			}

			return x("div", a("class", "doc_syntax"), x("code", parts));
		}

		static string Generate(IGrouping<string, MethodDom> group)
		{
			var first = group.First();
			var doc = GetDoc(out var body);
			body.Add(
				x("h1", first.Type.SimpleName + GetMemberName(first) + Names[Strings.SuffixDelimeter] +
					Names[GetMethodKindName(first)] + " " ),
				BuildMembers("OverloadList",group)
			);
			return doc.ToString();
		}

		static string GetMemberName(MemberDom member)
		{
			if (member is ConstructorDom)
				return null;
			else
				return "." + member.Name;
		}

		static string GetMethodKindName(MemberDom m)
		{
			if (m is MethodDom meth)
			{
				if (meth.IsOperator)
					return Strings.Operator;
				else
					return Strings.Method;
			}
			else if (m is ConstructorDom)
				return Strings.Constructor;
			else if (m is EventDom)
				return Strings.Event;
			else if (m is PropertyDom)
				return Strings.Property;
			else if (m is FieldDom)
				return Strings.Field;
			else 
				throw new Exception();
		}

		static XElement BuildReturns(MemberDom m)
		{
			if (!(m is MethodDom meth))
				return BuildMemberType(m);
			if (meth.MemberType==null || meth.MemberType == typeof(void))
				return null;
			return 
				x("div",
					a("class", "doc_return"),
					x("h4", Names[Strings.ReturnValue], a("class", "doc_h4")),
					Names[Strings.Type] + ": ",
					BuildTypeUrl(meth.MemberType),
					x("br"),
					XMLUtils.GetTagInnerXml(m.DocInfo,"returns",Navigation, true)
				);
		}
		static XElement BuildMemberType(MemberDom m)
		{
			if (m.MemberType == null)
				return null;
			return x("span",
			         Names[Strings.Type] + ": ", BuildTypeUrl(m.MemberType)
				);
		}
	}
}
