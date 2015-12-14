#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.DocViewer.DocViewer
File: BuildTree.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.DocViewer
{
	using System.Collections.Generic;
	using System.Linq;

	using XMLCommToHTM;
	using XMLCommToHTM.DOM;

	static class BuildPages
	{
		public static OfflineDynamicPage BuildSolution(SolutionDom solutionDom, bool onlyNamespaces = true)
		{
			var ret = new OfflineDynamicPage
			{
				RussianTitle = solutionDom.Name,
				RussianContent = GenerateHtml.Generate(solutionDom),
			};

			if (onlyNamespaces)
				AddNamespaces(ret, solutionDom.Namespaces);
			else
				foreach (var asm in solutionDom.Assemblies)
					AddAssembly(ret, asm);

			return ret;
		}

		private static void AddAssembly(OfflineDynamicPage rootItems, AssemblyDom asmDom)
		{
			var asmItem = new OfflineDynamicPage
			{
				Parent = rootItems,
				UrlPart = asmDom.Name,
				RussianTitle = asmDom.Name,
				RussianContent = GenerateHtml.Generate(asmDom),
			};
			rootItems.Childs.Add(asmItem);
			AddNamespaces(asmItem, asmDom.Namespaces);
		}

		private static void AddNamespaces(OfflineDynamicPage parentNode, IEnumerable<NamespaceDom> namespaces)
		{
			foreach (var namesp in namespaces)
			{
				var namespaceItem = new OfflineDynamicPage
				{
					Parent = parentNode,
					UrlPart = GenerateHtml.Navigation.UrlPrefix + namesp.Name.Replace('.', '/'),
					RussianTitle = namesp.Name,
					RussianContent = GenerateHtml.Generate(namesp)
				};
				AddTypes(namespaceItem, namesp.Types);
				parentNode.Childs.Add(namespaceItem);
			}
		}
		private static void AddTypes(OfflineDynamicPage parentNode, IEnumerable<TypeDom> types)
		{
			if (types == null || !types.Any())
				return;

			foreach (var type in types)
			{
				var typeItem = new OfflineDynamicPage
				{
					Parent = parentNode,
					UrlPart = GenerateHtml.Navigation.UrlPrefix + type.Namespace.Replace('.', '/') + "/" + type.SimpleName,
					RussianTitle = type.GetDisplayName(false),
					RussianContent = GenerateHtml.Generate(type),
				};

				if (type.NestedTypes != null && type.NestedTypes.Length > 0)
				{
					var nestedTypeItem = new OfflineDynamicPage
					{
						Parent = typeItem,
						UrlPart = typeItem.UrlPart + "/NestedTypes",
						RussianTitle = GenerateHtml.NestedTypesName,
						RussianContent = GenerateHtml.Generate(
							new TypePartialData { SectionType = MemberTypeSection.NestedTypes, Type = type }
							),
					};
					AddTypes(nestedTypeItem, type.NestedTypes);
					typeItem.Childs.Add(nestedTypeItem);
				}

				AddMembers(typeItem, type, type.Constructors, MemberTypeSection.Constructors);
				AddMembers(typeItem, type, type.Properties, MemberTypeSection.Properties);
				AddMembers(typeItem, type, type.Methods, MemberTypeSection.Methods);
				AddMembers(typeItem, type, type.ExtentionMethods, MemberTypeSection.ExtentionMethods);
				AddMembers(typeItem, type, type.Operators, MemberTypeSection.Operators);
				AddMembers(typeItem, type, type.Fields, MemberTypeSection.Fields);
				AddMembers(typeItem, type, type.Events, MemberTypeSection.Events);
				parentNode.Childs.Add(typeItem);
			}
		}

		private static void AddMembers(OfflineDynamicPage parentNode, TypeDom type, MemberDom[] members, MemberTypeSection section)
		{
			if (members == null || members.Length == 0)
				return;

			var sectionItem = new OfflineDynamicPage
			{
				Parent = parentNode,
				UrlPart = parentNode.UrlPart + "/" + section.ToString(),
				RussianTitle = GenerateHtml.GetSectionName(section),
				RussianContent = GenerateHtml.Generate(new TypePartialData { SectionType = section, Type = type }),
			};

			if (section == MemberTypeSection.Methods || section == MemberTypeSection.ExtentionMethods)
				GenerateMethods((MethodDom[])members, sectionItem, parentNode.UrlPart);
			else
			{
				foreach (var member in members)
				{
					//ToDo: Группировка переопределенных методов.
					var memberItem = new OfflineDynamicPage
					{
						Parent = sectionItem,
						UrlPart = parentNode.UrlPart + "/" + member.SimpleName,
						RussianTitle = member.ShortSignature,
						RussianContent = GenerateHtml.Generate(member),
					};
					sectionItem.Childs.Add(memberItem);
				}
			}

			parentNode.Childs.Add(sectionItem);
		}

		private static void GenerateMethods(IEnumerable<MethodDom> methods, OfflineDynamicPage parentNode, string typeUrl)
		{
			var groups = methods.GroupBy(_ => _.SimpleName).OrderBy(_ => _.Key);

			foreach (var member in groups)
			{
				string url = typeUrl + "/" + member.Key;

				string html = member.Count() == 1
					? GenerateHtml.Generate(member.Single())
					: GenerateHtml.Generate(member);

				var memberItem = new OfflineDynamicPage
				{
					Parent = parentNode,
					UrlPart = url,
					RussianTitle = member.Key,
					RussianContent = html,
				};

				if (1 < member.Count())
					GenerateOverloadMethods(member, memberItem, url);

				parentNode.Childs.Add(memberItem);
			}
		}

		private static void GenerateOverloadMethods(IEnumerable<MethodDom> methods, OfflineDynamicPage parentNode, string baseUrl)
		{
			foreach (var m in methods.OrderBy(_ => _.ShortSignature))
			{
				var item = new OfflineDynamicPage
				{
					Parent = parentNode,
					UrlPart = baseUrl + "/" + m.OverloadIndex.ToString(),
					RussianTitle = m.ShortSignature,
					RussianContent = GenerateHtml.Generate(m),
				};
				parentNode.Childs.Add(item);
			}
		}
	}
}