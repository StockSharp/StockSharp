#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: XMLCommToHTM.DOM.Internal.DOC.DocViewer
File: AssemblyDoc.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.XPath;

namespace XMLCommToHTM.DOM.Internal.DOC
{
	public class AssemblyDoc
	{
		public string Name;
		public TypeDoc[] Types;

		public static AssemblyDoc Parse(XElement doc)
		{
			return new AssemblyDoc
			{
				Name = doc.XPathSelectElement("assembly/name").Value,
				Types = ParseMembers(doc.XPathSelectElement("members"))
			};
		}

		//Если попадаются members у которых включающий их класс не был объявлен в XML документации, то создается класс, но у него не будет заполнено RawNode
		static TypeDoc[] ParseMembers(XElement members)
		{
			Dictionary<string, TypeDoc> types = members
				.XPathSelectElements("member[starts-with(@name,\"T:\")]")
				.Select(TypeDoc.Parse)
				.Where(t => !t.FullName.Contains("XamlGeneratedNamespace"))
				.ToDictionary(_ => _.FullName);

			foreach (var node in members.XPathSelectElements("member[not(starts-with(@name,\"T:\"))]"))
			{
				var member = MemberDoc.ParseMember(node);
				if (member != null)
				{
					if (types.TryGetValue(member.Name.FullClassName, out var type))
					{
						type.Members.Add(member);
					}
					else
					{
						type = new TypeDoc { FullName = member.Name.FullClassName };
						type.Members.Add(member);

						if (!type.FullName.Contains("XamlGeneratedNamespace"))
							types.Add(type.FullName, type);
					}
				}
				else
				{
					//ToDo: сделать обработку типов(редкие,неиспользуемые) 'N'-namespace, '!' - error
				}
			}

			return types
				.Select(_ => _.Value)
				.ToArray();
		}

	   
		/// <summary>
		/// 
		/// </summary>
		/// <param name="asm"></param>
		/// <param name="findOptions"> Опции указывающие какие типы и члены собирать</param>
		/// <param name="unboundTypes">Массив типов из XML документации, которые не смогли привязаться к данным из Reflection</param>
		/// <param name="unboundMembers">Массив members из XML документации, которые не смогли привязаться к данным из Reflection</param>
		public void MergeWithReflection(Assembly asm, FindOptions findOptions, out TypeDoc[] unboundTypes, out MemberDoc[] unboundMembers)
		{
			var docTypes=Types.ToDictionary(_ => _.FullName);
			foreach (var type in asm.GetTypes())
			{
				if(!type.IsVisible && !findOptions.InternalClasses)
					continue;

				string name = type.FullName.Replace('+', '.'); //Для nested классов type.FullName дает Class1+Class2, а по XML Class1.Class2  - невозможно отличить nested класс от namespace
				if (docTypes.TryGetValue(name, out var docT))
				{
					if (docT.ReflectionType != null)
						throw new Exception("multiple types on single node");
					docT.ReflectionType = type;
				}
				else if (findOptions.UndocumentedClasses)
				{
					docT = new TypeDoc { FullName = name, ReflectionType = type };

					if (!docT.FullName.Contains("XamlGeneratedNamespace"))
						docTypes.Add(docT.FullName, docT);
				}
			}

			if (findOptions.UndocumentedClasses)
				Types = docTypes.Select(_ => _.Value).ToArray();

			unboundTypes = Types.Where(_ => _.ReflectionType == null).ToArray();
			if (unboundTypes.Length > 0)
				Types = Types.Where(_ => _.ReflectionType != null).ToArray();

			var unbound = new List<MemberDoc>();
			foreach (var type in Types)
				unbound.AddRange(type.MergeMembersWithReflection(!findOptions.UndocumentedMembers, findOptions.PrivateMembers));
			unboundMembers = unbound.ToArray();
		}


	}

}
