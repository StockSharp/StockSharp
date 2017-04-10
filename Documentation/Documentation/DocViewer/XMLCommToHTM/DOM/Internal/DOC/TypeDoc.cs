#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: XMLCommToHTM.DOM.Internal.DOC.DocViewer
File: TypeDoc.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace XMLCommToHTM.DOM.Internal.DOC
{
	public class TypeDoc
	{
		public XElement DocInfo;
		public string FullName;
		public Type ReflectionType;
		public readonly List<MemberDoc> Members = new List<MemberDoc>();

		public static TypeDoc Parse(XElement element)
		{
			var ret = new TypeDoc
				{
					FullName = element.Attribute("name").Value.Substring(2), 
					DocInfo = element
				};
			return ret;
		}

		/// <summary>
		/// Привязывает members к данным из Reflection
		/// </summary>
		/// <param name="onlyDocumentedMembers">true - добавлять члены из Reflection, которых нет в XML документации</param>
		/// <param name="privateMembers"></param>
		/// <returns>members которые не привязались к данным из Reflection</returns>
		public MemberDoc[] MergeMembersWithReflection(bool onlyDocumentedMembers, bool privateMembers)
		{
			MergeMethodsWithReflection(onlyDocumentedMembers,privateMembers);
			MergeFieldsWithReflection(onlyDocumentedMembers,privateMembers);
			MergePropertiesWithReflection(onlyDocumentedMembers,privateMembers);
			return Members.Where(_ => _.ReflectionMemberInfo == null).ToArray();
		}
		void AddNewMembers(IEnumerable<MemberInfo> members)
		{
			Members.AddRange(members
					.Where(_ => _ != null)
					.Select(_ => new MemberDoc { ReflectionMemberInfo = _ })
					);
		}

		class EqComp : IEqualityComparer<MemberInfo>
		{
			public bool Equals(MemberInfo x, MemberInfo y)
			{
				return x.Name == y.Name;
			}

			public int GetHashCode(MemberInfo obj)
			{
				return obj.Name.GetHashCode();
			}
		}

		void MergePropertiesWithReflection(bool onlyDocumentedMembers, bool privateMembers)
		{
			var props=ReflectionType
				.GetProperties(AllMembersBindFlags)
				.Where(_ => 
					privateMembers || 
					(_.GetMethod != null && !_.GetMethod.IsPrivate) ||
					(_.SetMethod != null && !_.SetMethod.IsPrivate)
				)
				.ToArray();
			var propsDoc = Members.Where(_ => _.Type == MemberType.Property).ToArray();

			for (int i = 0; i < props.Length; i++)
				foreach (var propDoc in propsDoc)
				{
					PropertyInfo prop = props[i];
					if (DoPropertiesMatch(prop, propDoc))
					{
						if (propDoc.ReflectionMemberInfo != null)
						{
							prop = (PropertyInfo)GetMostDerivedMember(prop, propDoc.ReflectionMemberInfo);
							if (prop == null)
								throw new Exception("Error. Multiple binding.");
						}
						propDoc.ReflectionMemberInfo = prop;
						props[i] = null;
						break;
					}
				}
			if (!onlyDocumentedMembers)
				AddNewMembers(props);
		}


		static bool DoPropertiesMatch(PropertyInfo mi, MemberDoc mDoc)
		{
			string miName = TypeUtils.ReplacePlus(mi.Name);
			if (miName != mDoc.Name.MemberName)
				return false;
			if (!TypeUtils.DoParametersMatch(mi.GetIndexParameters(), mDoc.Arguments ?? EmptyStr))
				return false;
			return true;
		}

		private const BindingFlags AllMembersBindFlags = BindingFlags.Public | BindingFlags.NonPublic
			| BindingFlags.Instance | BindingFlags.Static /*| BindingFlags.DeclaredOnly*/;
		private void MergeFieldsWithReflection(bool onlyDocumentedMembers, bool privateMembers)
		{
			MemberInfo[] fes =
				ReflectionType
					.GetFields(AllMembersBindFlags)
					.Where(_ => privateMembers || !_.IsPrivate)
					.Where(_ => !ReflectionType.IsEnum || _.Name != "value__").Concat<MemberInfo>(ReflectionType
						.GetEvents(AllMembersBindFlags)
						.Where(_ => 
							privateMembers || 
							(_.AddMethod!=null && !_.AddMethod.IsPrivate) ||
							(_.RemoveMethod!=null && !_.RemoveMethod.IsPrivate)
						)
					)
					.Distinct(new EqComp()) //Убрать дубли - простые события возвращаются и GetFields и GetEvents, события с add,remove только через GetEvents
					//.Where(_ => privateMembers || !_.IsPrivate)
					.ToArray();
			var fieldsdsDoc = Members.Where(_ => _.Type == MemberType.Field || _.Type == MemberType.Event).ToArray();

			for (int i = 0; i < fes.Length; i++)
				foreach (var fieldDoc in fieldsdsDoc)
				{
					MemberInfo fe = fes[i];
					if (fe.Name == fieldDoc.Name.MemberName)
					{
						if (fieldDoc.ReflectionMemberInfo != null)
						{
							fe = GetMostDerivedMember(fe, fieldDoc.ReflectionMemberInfo);
							if(fe==null)
								throw new Exception("Error. Multiple binding.");
						}
						fieldDoc.ReflectionMemberInfo = fe;
						fes[i] = null;
						break;
					}
				}
			if (!onlyDocumentedMembers)
				AddNewMembers(fes);
		}

		void MergeMethodsWithReflection(bool onlyDocumentedMembers, bool privateMembers)
		{
			var methods = (ReflectionType.IsEnum ? Enumerable.Empty<MethodBase>() :
				ReflectionType.GetMethods(AllMembersBindFlags)
				.Concat<MethodBase>(ReflectionType.GetConstructors(AllMembersBindFlags))
					.Where(MemberUtils.IsVisibleMethod)
					.Where(_ => privateMembers || !_.IsPrivate))
					.ToArray();
			var methodsDoc = Members.Where(_ => _.Type == MemberType.Method).ToArray();
			for(int i=0; i<methods.Length;i++)
				foreach (var methodDoc in methodsDoc)
				{
					MethodBase method = methods[i];
					if (DoMethodsMatch(method, methodDoc))
					{
						//если одинаковые имена но DeclaringType различаются, то это может быть переопределенный через new
						if (methodDoc.ReflectionMemberInfo != null)
						{
							method=(MethodBase)GetMostDerivedMember(method, methodDoc.ReflectionMemberInfo);
							if(method==null)
								throw new Exception("Error. Multiple binding.");
						}
						methodDoc.ReflectionMemberInfo = method;
						methods[i] = null;
						break; //? ToDo: как для базовых классов
					}
				}
			if (!onlyDocumentedMembers)
				AddNewMembers(methods);
		}

		static MemberInfo GetMostDerivedMember(MemberInfo m1, MemberInfo m2)
		{
			if (m1.DeclaringType == m2.DeclaringType)
				return null;
			if (TypeUtils.IsDerived(m1.DeclaringType, m2.DeclaringType))
				return m1;
			if (TypeUtils.IsDerived(m2.DeclaringType, m1.DeclaringType))
				return m2;
			return null;
		}
		
		static readonly string[] EmptyStr=new string[0];
		static bool DoMethodsMatch(MethodBase mi, MemberDoc mDoc)
		{
			string miName = TypeUtils.ReplacePlus(mi.Name);
			if (miName != mDoc.Name.MemberName)
				return false;

			int genericArgsCount = 0;
			if (mi.IsGenericMethod)
				genericArgsCount = mi.GetGenericArguments().Length;
			if (mDoc.GenericParameterCount != genericArgsCount)
				return false;

			if (!TypeUtils.DoParametersMatch(mi.GetParameters(), mDoc.Arguments??EmptyStr))
				return false;
			if ((mDoc.Name.MemberName == "op_Explicit" || mDoc.Name.MemberName == "op_Implicit") && 
				((MethodInfo)mi).ReturnType.FullName != mDoc.Returns
				)
				return false;
			
			return true;
		}

	}

}



