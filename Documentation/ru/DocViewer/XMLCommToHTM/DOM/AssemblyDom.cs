using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

using XMLCommToHTM.DOM.Internal;

namespace XMLCommToHTM.DOM
{
	using Internal.DOC;

	public class FindOptions
	{
		public bool UndocumentedClasses = true; // Собирать классы даже те которые не присутствуют в XML документации.
		public bool InternalClasses = false;    //Собирать internal классы
		public bool UndocumentedMembers = true; // Собирать члены классов даже те которые не присутствуют в XML документации.
		public bool PrivateMembers = false;     //Собирать private члены 
	}

	public class AssemblyDom
	{
		public Assembly ReflectionAssembly;

		private string _name;

		public string Name
		{
			get { return _name; }
		}

		/// <summary>
		/// Все типы во всех namespace'ах. А также nested классы. В виде плоского списка. 
		/// Эти же классы можно лежат в соответствующих Namespaces
		/// </summary>
		public TypeDom[] AllTypes; 
		public NamespaceDom[] Namespaces;

		public MemberDoc[] ErrorUnboundMembers;    //Массив members из XML документации, которые не смогли привязаться к данным из Reflection
		public TypeDoc[] ErrorUnboundTypes;         //Массив типов из XML документации, которые не смогли привязаться к данным из Reflection

		public string RuntimeVersion
		{
			get { return ReflectionAssembly.ImageRuntimeVersion; }
		}
		public string Version
		{
			get { return ReflectionAssembly.GetName().Version.ToString(); }
		}
		//public string FileName
		//{
		//	get { return Path.GetFileName(ReflectionAssembly.Location); }
		//}


		public static AssemblyDom Build(XElement doc, Assembly asm, Func<MemberDom, bool> filterMembers, FindOptions findOptions)
		{
			var asmDoc=AssemblyDoc.Parse(doc);
			return Build(asmDoc, asm, filterMembers, findOptions);
		}
		public static AssemblyDom Build(AssemblyDoc asmDoc, Assembly asm, Func<MemberDom, bool> filterMembers, FindOptions findOptions)
		{
			var ret = new AssemblyDom { _name = asmDoc.Name, ReflectionAssembly = asm };
			asmDoc.MergeWithReflection(asm, findOptions, out ret.ErrorUnboundTypes, out ret.ErrorUnboundMembers);
			ret.AllTypes = asmDoc.Types.Select(_ => TypeDom.Build(_, ret, filterMembers)).ToArray();
			ret.FillNamespaces();
			ret.FillNestedTypes();
			return ret;			
		}
		
		void FillNamespaces()
		{
			var nsDict = new Dictionary<string, List<TypeDom>>();
			foreach (var type in AllTypes.Where(_ => _.Type.DeclaringType == null))
			{
				List<TypeDom> nsTypes;
				if (nsDict.TryGetValue(type.Namespace, out nsTypes))
				{
					nsTypes.Add(type);
				}
				else
				{
					nsTypes = new List<TypeDom>{type};
					nsDict.Add(type.Namespace,nsTypes);
				}
			}
			Namespaces = nsDict
				.Select(_ =>
				{
					var ns = new NamespaceDom(_.Key);
					ns.Types.AddMany(_.Value);
					return ns;
				})
				.ToArray();
		}

		void FillNestedTypes()
		{
			var dict = AllTypes.ToDictionary(_ => TypeUtils.GetNameWithNamespaceShortGeneric(_.Type));
			foreach (var typeDom in AllTypes.Where(_=>_.Type.DeclaringType==null))
				FillNestedTypesRec(dict,typeDom);
		}

		static void FillNestedTypesRec(Dictionary<string, TypeDom> dict, TypeDom typeDom)
		{
			foreach (var ntype in typeDom.Type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
			{
				TypeDom nestedTypeDom;
				if (dict.TryGetValue(TypeUtils.GetNameWithNamespaceShortGeneric(ntype), out nestedTypeDom))
				{
					if(typeDom.NestedTypes==null)
						typeDom.NestedTypes=new TypeDom[0];
					typeDom.NestedTypes = typeDom.NestedTypes
						.Concat(Enumerable.Repeat(nestedTypeDom, 1))
						.ToArray();
					FillNestedTypesRec(dict, nestedTypeDom);
				}
			}
		}
		
	}

}
