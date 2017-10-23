#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: XMLCommToHTM.DOM.Internal.DocViewer
File: TypeUtils.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace XMLCommToHTM.DOM.Internal
{
	public static class TypeUtils
	{
		public static string SimpleName(Type type)
		{
			if (type.Name.Contains("`"))
				return type.Name.Split('`')[0];
			else
				return type.Name;
		}
		/*
		public static string SimpleNameWithNestedGen(Type type)
		{
			string ret = "";
			if (type.DeclaringType != null)
				ret += SimpleNameWithNestedGen(type.DeclaringType)+".";
			ret += type.Name.Split('`')[0];
			if (type.IsGenericType)
			{
				Type[] genArgs = GetGenericArgumentsOnlyOwn(type, type.GetGenericArguments());
				if (0 < genArgs.Length)
					ret += "(" + genArgs.Select(_ => _.Name).Aggregate((p1, p2) => p1 + "," + p2) + ")";
			}
			return ret;
		}
		*/
		public static string ToDocString(Type type, char openGen = '{', char closeGen = '}')
		{
			return ToDocStringRec(type, type.GetGenericArguments(), openGen, closeGen);
		}

		/// <summary>
		/// Конвртирует type в строковое описание типа, совместимое со строками в XML документации.
		/// Для Generic типов  есть суффиксы: '`(для generic параметров класса) и ``2 (для generic параметров функции)
		/// 
		/// Примечание: передается массив genericArguments, вместо того чтобы в функции вызывать type.GetGenericArguments()
		///     из-за того что функция рекурсивно вызывает родительский класс type.DeclaringType и передает ему часть аргументов
		///     И нельзя вызывать type.DeclaringType.GetGenericArguments() , т.к. вместо реальных аргументов окажутся объявленные
		///     Например, (угловые скобки заменены на {}) 
		///     вместо Class1{int,int}.Nested{int} , окажется Class1{T1,T2}.Nested{int} , как было в объявлении: class Class1{T1,T2} ....
		/// </summary>
		/// <param name="type"></param>
		/// <param name="genericArguments"></param>
		/// <param name="openGen"></param>
		/// <param name="closeGen"></param>
		/// <returns></returns>
		public static string ToDocStringRec(Type type, Type[] genericArguments, char openGen, char closeGen)
		{
			if (type.IsArray)
			{
				string typeStr = ToDocStringRec(type.GetElementType(), genericArguments, openGen, closeGen);
				string brackets = "[" + new string(',', type.GetArrayRank() - 1) + "]";
				return typeStr + brackets;
			}
			if (type.IsGenericParameter)
				return (type.DeclaringMethod == null ? "`" : "``") + type.GenericParameterPosition.ToString();
			if (type.IsGenericType)
			{
				string ret2;
				if (type.DeclaringType == null)
					ret2 = type.Namespace;
				else
				{
					var parentArgs = genericArguments.Take(type.DeclaringType.GetGenericArguments().Length).ToArray();
					ret2 = ToDocStringRec(type.DeclaringType, parentArgs, openGen, closeGen);
				}
				ret2 += "." + SimpleName(type);

				var genericArgs = GetGenericArgumentsOnlyOwn(type, genericArguments);
				if (genericArgs.Length > 0)
					ret2 +=
						 openGen +
						 genericArgs
						 .Select(_ => ToDocStringRec(_, _.GetGenericArguments() /*???*/, openGen, closeGen)).Aggregate((s1, s2) => s1 + "," + s2)
						+ closeGen;
				return ret2;
			}
			return ReplacePlus(type.FullName);
		}

		public static string ToDisplayString(Type type, bool includeNamespace, string brOpen = "<", string brClose = ">")
		{
			return ToDisplayString(type, type.GetGenericArguments(), includeNamespace, brOpen, brClose);
		}
		static string ToDisplayString(Type type, Type[] genericArguments, bool includeNamespace, string brOpen, string brClose)
		{
			if (type.IsArray)
			{
				string brackets = "";
				while (type.IsArray)
				{
					brackets = brackets + "[" + new string(',', type.GetArrayRank() - 1) + "]"; //добавляется в обратном порядке
					type = type.GetElementType();
				}
				return ToDisplayString(type, genericArguments, includeNamespace, brOpen, brClose) + brackets;
			}
			if (type.IsGenericParameter)
				return type.Name;//(type.DeclaringMethod == null ? "`" : "``") + type.GenericParameterPosition.ToString();
			if (type.IsGenericType)
			{
				string ret2 = "";
				if (type.DeclaringType != null)
				{
					var parentArgs = genericArguments.Take(type.DeclaringType.GetGenericArguments().Length).ToArray();
					ret2 = ToDisplayString(type.DeclaringType, parentArgs, includeNamespace, brOpen, brClose) + ".";
				}
				else if (includeNamespace)
					ret2 += type.Namespace + ".";
				ret2 += SimpleName(type);

				var genericArgs = GetGenericArgumentsOnlyOwn(type, genericArguments);
				if (genericArgs.Length > 0)
					ret2 +=
						 brOpen +
						 genericArgs
						 .Select(_ => ToDisplayString(_, _.GetGenericArguments(), includeNamespace, brOpen, brClose)).Aggregate((s1, s2) => s1 + "," + s2)
						+ brClose;
				return ret2;
			}

			return (includeNamespace ? type.Namespace + "." : "") + ReplacePlus(type.Name);
		}


		//Для nested классов с generic параметрами, если у parent класса тоже есть generic параметры, возвращает только свои для Nested класса параметры.
		//Если не реализовать, в указанном случае не привязывается. Вместо Parent<T1>.Child<T2>, получается Parent<T1,T2>.Child
		//(Функция type.GetGenericArguments() возвращает все и от parent класса)
		//Не нашлось в API такого метода, пришлось вручную - исключать параметры parent класса
		static Type[] GetGenericArgumentsOnlyOwn(Type type, Type[] genericArgs)
		{
			if (type.DeclaringType != null)
				genericArgs = genericArgs.Skip(type.DeclaringType.GetGenericArguments().Length).ToArray();
			return genericArgs;
		}


		/// <summary>
		/// Для имен nested классов Class1+Class2 заменяет на Class1.Class2
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static string ReplacePlus(string s)
		{
			if (String.IsNullOrEmpty(s))
				return s;
			if (s.Contains('+'))
				s = s.Replace('+', '.');
			return s;
		}


		public static bool DoParametersMatch(ParameterInfo[] args, string[] argsDoc)
		{
			if (args.Length != argsDoc.Length)
				return false;
			if (args.Length == 0)
				return true;
			for (int i = 0; i < args.Length; i++)
			{
				//var debug = TypeToString.ToString(args[i].ParameterType);
				if (ToDocString(args[i].ParameterType) != argsDoc[i])
					return false;
			}
			return true;
		}

		//Пример: Namespace1.Namespace2.Class1`2.Nested`1
		public static string GetNameWithNamespaceShortGeneric(Type type)
		{
			/*
			Type curType = type;
			string ret=curType.Name;
			curType = curType.DeclaringType;
			while (curType != null)
			{
				ret += curType.Name+".";
				curType = curType.DeclaringType;
			}
			*/
			string ret = type.FullName;
			if (type.Namespace != null)
				ret = type.Namespace + "." + ret;
			return ret;
		}

		public static bool IsDerived(Type derivedType, Type baseType)
		{
			return null != GetBaseTypes(derivedType).FirstOrDefault(_ => _ == baseType);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="type"></param>
		/// <returns>Первый элемент непосредственный базовый класс, последний самый базовый</returns>
		public static IEnumerable<Type> GetBaseTypes(this Type type)
		{
			if (type == null)
				yield break;
			type = type.BaseType;
			while (type != null)
			{
				yield return type;
				type = type.BaseType;
			}
		}


		///// <summary>
		///// 
		///// </summary>
		///// <remarks>
		///// Extension method must be defined in a top level, static,non generic class
		///// </remarks>
		///// <param name="types"></param>
		///// <returns></returns>
		//public static MethodInfo[] GetExtentionMethods(IEnumerable<Type> types)
		//{
		//	return types
		//		.Where(_ => CanHaveExtentionMethods(_))
		//		.SelectMany(_ => _.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
		//		.Where(_ => _.IsDefined(typeof(ExtensionAttribute), false) && _.GetParameters().Length>0)
		//		.ToArray();
		//}

		//

		/// <summary>
		/// Extension method must be defined in a top level, static,non generic class
		/// статические классы в reflection имеют только признаки IsAbstract,IsSealed , других признаков статического класса нет.
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		public static bool CanHaveExtentionMethods(Type t)
		{
			return !t.IsNested && !t.IsGenericType && t.IsAbstract && t.IsSealed;
		}

		public static Type GetRootElementType(Type type)
		{
			Type elType = type.GetElementType();
			if (elType == null)
				return null;
			while (elType.IsArray)
				elType = elType.GetElementType();
			return elType;
		}

		private class SimpleTypeComparer : IEqualityComparer<Type>
		{
			public bool Equals(Type x, Type y)
			{
				return x.Assembly == y.Assembly &&
					x.Namespace == y.Namespace &&
					x.Name == y.Name;
			}

			public int GetHashCode(Type obj)
			{
				throw new NotImplementedException();
			}
		}

		public static MethodInfo GetGenericMethod(this Type type, string name, Type[] parameterTypes)
		{
			var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (var method in methods.Where(m => !m.IsPrivate && m.Name == name))
			{
				var methodParameterTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();

				if (methodParameterTypes.SequenceEqual(parameterTypes, new SimpleTypeComparer()))
				{
					return method;
				}
			}

			return null;
		}
	}
}
