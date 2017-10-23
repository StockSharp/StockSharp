#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: XMLCommToHTM.DOM.DocViewer
File: SolutionDom.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using XMLCommToHTM.DOM.Internal;
using XMLCommToHTM.DOM.Internal.DOC;

namespace XMLCommToHTM.DOM
{
	using Ecng.Collections;

	public class WarningOrError
	{
		public enum ErrorTypeEn
		{
			BindingWithReflection,
			HaveUnboundTypesOrMembers
		};
		public ErrorTypeEn ErrorType;
		public Exception InnerException;
		public string AssemblyName;
		//public string Text;
		public TypeDoc[] UnboundTypes;
		public MemberDoc[] UnboundMembers;
	}
	public class SolutionDom
	{
		public AssemblyDom[] Assemblies;
		public NamespaceDom[] Namespaces;
		public TypeDom[] AllTypes; //те же типы находятся в Namespaces...
		public List<WarningOrError> Errors;

		public string Name { get; private set; }

		public static SolutionDom Build(string name, string[] assemblyFiles, string[] xmlDocFiles, string xmlNamespaceProjectFile = null, Func<MemberDom, bool> filterMembers = null, FindOptions findOptions = null)
		{
			if (findOptions == null)
				findOptions = new FindOptions(); //использовать значения по умолчанию

			IEnumerable<AssemblyDoc> docs = ParseDocfiles(xmlDocFiles);
			var asmsDict= AssemblyUtils.LoadAllAssemblies(assemblyFiles)
				.ToDictionary(_=>_.GetName().Name);
			var asmDoms = new List<AssemblyDom>();
			var errors = new List<WarningOrError>();
			foreach (var doc in docs)
			{
				AssemblyDom asmDom = null;
				try
				{
					asmDom = AssemblyDom.Build(doc, asmsDict[doc.Name], filterMembers, findOptions );
				}
				catch (Exception ex)
				{
					errors.Add(new WarningOrError{AssemblyName = doc.Name, InnerException = ex, ErrorType =WarningOrError.ErrorTypeEn.BindingWithReflection}); 
				}
				if (asmDom != null)
				{
					asmDoms.Add(asmDom);
					var error=GetUnboundError(asmDom);
					if(error!=null)
						errors.Add(error);
				}
			}
			var ret = new SolutionDom {Assemblies = asmDoms.ToArray(), Errors = errors, Name = name };
			ret.Namespaces = MergeNamespaces(ret.Assemblies.SelectMany(_ => _.Namespaces));
			ret.AllTypes = ret.Assemblies.SelectMany(_ => _.AllTypes).ToArray();
				//ret.Namespaces.SelectMany(_ => _.Types).ToArray();

			PopulateExtentionMethods.Populate(ret.AllTypes);
			MergeDocWithBaseClasses.Merge(ret.AllTypes);

			//namespaces
			if (xmlNamespaceProjectFile != null)
			{
				var comm = NamespaceCommentsParser.Parse(XDocument.Load(xmlNamespaceProjectFile).Root);

				foreach (var ns in ret.Namespaces)
				{
					if (comm.TryGetValue(ns.Name, out var c))
						ns.DocInfo = c.Comments;
				}
			}

			foreach (var type in ret.AllTypes)
				type.FillOverrideIndex();
			return ret;
		}

		static WarningOrError GetUnboundError(AssemblyDom asmDom)
		{
			if (
				(asmDom.ErrorUnboundTypes == null || asmDom.ErrorUnboundTypes.Length == 0) &&
				(asmDom.ErrorUnboundMembers == null || asmDom.ErrorUnboundMembers.Length == 0)
				)
				return null;
			return new WarningOrError
				{
					ErrorType = WarningOrError.ErrorTypeEn.HaveUnboundTypesOrMembers,
					AssemblyName = asmDom.Name,
					UnboundTypes = asmDom.ErrorUnboundTypes,
					UnboundMembers = asmDom.ErrorUnboundMembers
				};

		}
		static NamespaceDom[] MergeNamespaces(IEnumerable<NamespaceDom> namespaces)
		{
			var nsDict = new Dictionary<string, NamespaceDom>();
			
			foreach (var ns in namespaces)
			{
				var curNs = nsDict.SafeAdd(ns.Name, name => new NamespaceDom(name));
				curNs.Types.AddRange(ns.Types);
			}

			return nsDict.Values.OrderBy(_ => _.Name).ToArray();
		}

		static IEnumerable<AssemblyDoc> ParseDocfiles(IEnumerable<string> xmlDocFiles)
		{
			var docs = new List<AssemblyDoc>();
			foreach (var docFile in xmlDocFiles)
			{
				AssemblyDoc asmDoc = null;
				try
				{
					var xDoc = XElement.Load(docFile);
					asmDoc = AssemblyDoc.Parse(xDoc);
				}
				catch (Exception ex)
				{
					//ToDo: Обработать ошибку
				}
				if (asmDoc != null)
					docs.Add(asmDoc);
			}
			return docs.ToArray();
		}
		/*
		/// <summary>
		/// 
		/// </summary>
		/// <param name="dotName">
		/// Имя namespace,класса, или члена. Части имени разделены точкой. Без generic символа '`'  
		/// Возвращаемый тип, один из: NamespaceDom, TypeDom, MemberDom, MemberDom[]
		/// После имени переопределенного члена может идти чрез точку номер переопределенного члена, 1 based (1,2,3,4...).
		/// Если номер переопределеного члена не указан и их несколько, то возвращается массив.
		/// Например: "namespace", "namespace1.namespace2.Class1.Class2.Member.1"
		/// </param>
		/// <returns></returns>
		public object FindItem(string dotName)
		{
			string[] nameParts = dotName.Split('.');

			string nameSpace = "";
			NamespaceDom nsDom = null;
			int nextIndex = -1;
			for (int i = 0; i < nameParts.Length; i++)
			{
				if (i != 0)
					nameSpace += ".";
				nameSpace += nameParts[i];
				var n=Namespaces.SingleOrDefault(_ => _.Name == nameSpace);
				if (n == null)
					break;
				nsDom = n; //останется последний самый длинный namespace
				nextIndex = i+1;
			}
			if(nsDom==null)
				throw new Exception();
			if (nextIndex == nameParts.Length)
				return nsDom;

			// Получение класса.
			TypeDom typeDom = nsDom.Types.Find(_ => _.SimpleName==nameParts[nextIndex++]);
			if(typeDom==null)
				throw new Exception();
			if (nextIndex == nameParts.Length)
				return typeDom;
			for (; nextIndex < nameParts.Length; nextIndex++)
			{
				var td = typeDom.NestedTypes.FirstOrDefault(_ => _.SimpleName == nameParts[nextIndex]);
				if(td==null)
					break;
				typeDom = td;
			}
			if (nextIndex == nameParts.Length)
				return typeDom;

			//Получение члена.
			MemberDom[] memAr=typeDom.AllMembers.Where(_ => _.SimpleName == nameParts[nextIndex++]).ToArray();
			if (nextIndex == nameParts.Length)
			{
				if (memAr.Length == 1)
					return memAr[0];
				else 
					return memAr;
			}
			if (nextIndex != nameParts.Length-1)
				throw new Exception(); //может оставаться только один элемент - номер override
			return memAr[int.Parse(nameParts[nextIndex]) - 1];
		}
		 */
	}
}
