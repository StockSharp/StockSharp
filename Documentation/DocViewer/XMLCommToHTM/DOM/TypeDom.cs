#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: XMLCommToHTM.DOM.DocViewer
File: TypeDom.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
using System;
using System.Linq;
using System.Xml.Linq;

using XMLCommToHTM.DOM.Internal;
using XMLCommToHTM.DOM.Internal.DOC;

namespace XMLCommToHTM.DOM
{
	public class TypeDom
	{
		public enum TypeKindEnum
		{
			Class,
			Struct,
			Interface,
			Delegate,
			Enum
		};

		public AssemblyDom Assembly;
		public XElement DocInfo;
		public Type Type;

		public GenericParameterDom[] GenericTypeParameters;
		public MemberDom[] AllMembers;
		
		public ConstructorDom[] Constructors;
		public FieldDom[] Fields;
		public PropertyDom[] Properties;
		public MethodDom[] Methods;
		public MethodDom[] Operators;
		public EventDom[] Events;

		public MethodDom[] ExtentionMethods;


		public TypeDom[] NestedTypes;

		public TypeKindEnum TypeKind
		{
			get
			{
				if(Type.BaseType==typeof(MulticastDelegate))
					return TypeKindEnum.Delegate;
				if (Type.IsInterface)
					return TypeKindEnum.Interface;
				else if(Type.IsEnum)
					return TypeKindEnum.Enum;
				else if (Type.IsValueType)
					return TypeKindEnum.Struct;
				return TypeKindEnum.Class;
			}
		}

		public string FullName => Type.FullName;
		public string Namespace => Type.Namespace??"";
		public string SimpleName => TypeUtils.SimpleName(Type);
		//public string SimpleNameWithNestedGen { get { return TypeUtils.ToDisplayString(Type,false,"(",")"); } } 
		public string GetDisplayName(bool includeNamespace, string brOpen="<", string brClose=">")
		{
			return TypeUtils.ToDisplayString(Type, includeNamespace, brOpen, brClose);
		}

		public Type[] BaseTypes => Type.GetBaseTypes()
			//.Select(_ => TypeUtils.ToDisplayString(_, true))
			.Reverse()
			.ToArray();

		//public string[] GetDistinctMethodsNames()
		//{
		//	return Methods.Select(_ => _.Name).Distinct().ToArray();
		//}

		public Type[] DerivedTypes => AssemblyUtils.GetAllDerivedTypes(Assembly.ReflectionAssembly, Type).ToArray();

		public static TypeDom Build(TypeDoc doc, AssemblyDom asm, Func<MemberDom, bool> filterMembers)
		{
			var ret = new TypeDom
				{
					Assembly = asm,
					DocInfo = doc.DocInfo,
					Type = doc.ReflectionType,
				};
			MemberDom[] members = doc.Members
									 .Where(_ => _.ReflectionMemberInfo != null)
									 .Select(_ => MemberDom.Build(ret, _.ReflectionMemberInfo, _.DocInfo))
									 .ToArray();
			
			members = members.Where(
				_ => (filterMembers==null || filterMembers(_)) && !_.IsPrivateOrInternal
				).ToArray();
			
			ret.AllMembers = members;
			ret.Constructors = members.OfType<ConstructorDom>().OrderBy(_ => _.ShortSignature).ToArray();
			ret.Fields = members.OfType<FieldDom>()
			                .OrderBy(_ => _.ShortSignature)
			                .ToArray();
			ret.Properties = members.OfType<PropertyDom>()
			                .OrderBy(_ => _.ShortSignature)
			                .ToArray();
			ret.Methods = members.OfType<MethodDom>()
			                .Where(_ => !_.IsOperator)
			                .OrderBy(_ => _.ShortSignature)
			                .ToArray();
			ret.Operators = members.OfType<MethodDom>()
			                .Where(_ => _.IsOperator)
			                .OrderBy(_ => _.ShortSignature)
			                .ToArray();
			ret.Events = members.OfType<EventDom>()
			                .OrderBy(_ => _.ShortSignature)
			                .ToArray();
			ret.GenericTypeParameters = GenericParameterDom.BuildTypeGenericParameters(ret.Type, ret.DocInfo);
			return ret;
		}

		public string Name => Type.Name;

		public void FillOverrideIndex()
		{
			var groups = Methods
				.GroupBy(_ => _.SimpleName)
				.Where(_=>_.Count()>1);
			foreach (var g in groups)
			{
				int index = 1;
				foreach (var methodDom in g.OrderBy(_ => _.ShortSignature))
				{
					methodDom.OverloadIndex = index;
					index++;
				}
			}
		}
	}
}
