#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: XMLCommToHTM.DOM.Internal.DocViewer
File: PopulateExtentionMethods.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
using System;
using System.Collections.Generic;
using System.Linq;

namespace XMLCommToHTM.DOM.Internal
{
	public static class PopulateExtentionMethods
	{
		public static void Populate(TypeDom[] allTypes)
		{
			ILookup<Type, MethodDom> extentionsLookup = GetExtentionMethods(allTypes)
				.ToLookup(_ => _.FirtParameterType);
			foreach (var typeDom in allTypes)
				typeDom.ExtentionMethods = 
					GetExtentionMethods(typeDom.Type, extentionsLookup)
					.OrderBy(_ => _.FirtParameterType.FullName ?? _.FirtParameterType.Name) //Для generic параметра FullName может быть null
					.ToArray();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <remarks>
		/// Extension method must be defined in a top level, static,non generic class
		/// </remarks>
		/// <param name="types"></param>
		/// <returns></returns>
		static MethodDom[] GetExtentionMethods(IEnumerable<TypeDom> types)
		{
			return types
				.Where(_ => TypeUtils.CanHaveExtentionMethods(_.Type))
				.SelectMany(_ => _.Methods)
				.Where(_ => _.IsExtention)
				.ToArray();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <remarks>
		/// Может быть: Method(this T), где T базовый класс, базовый интерфейс. Поэтому может быть несколько одноименных методов.
		/// 
		/// Правила C#:
		/// Сначала собираются все extention мтоды для всех базовых классов и интерфейсов от которых наследуется класс. 
		/// Если попадаются одноименнные для них неоднозначность разрешается так (по типу который они расширяют):
		/// Выбираются более приоритетно расширения для базовых классов, берется наиболее derived
		/// если базовых классов нет то, потом для интерфейсов(независимо где по иерархии интерфейсы добавлены в наследование)
		/// Если выбраны расширения для нескольких интерфейсов, выбирается наиболее derived. 
		/// Если же нет наиболее derived интерфейса(для которого все остальные базовые) - то ошибка.
		/// 
		/// ToDo: Из них надо бы выбирать наиболее derived, но пока собираются все.
		/// Осложняется тем что, для класса может быть неоднозначный вызов. Например если класс наследуется от интерфейсов I1,I2,
		/// и Extention методы определены и для I1 и для I2, вызов для этого класса будет ошибочный, но показывать что они есть, наверно все равно надо.
		/// </remarks>
		/// <param name="type"></param>
		/// <param name="lookup"></param>
		/// <returns></returns>
		static IEnumerable<MethodDom> GetExtentionMethods(Type type, ILookup<Type, MethodDom> lookup)
		{
			var baseTypes =
				Enumerable.Repeat(type, 1)
				.Concat(type.GetBaseTypes())
				.Concat(type.GetInterfaces());
			var res = new List<MethodDom>();
			foreach (var baseType in baseTypes)
			{
				if (lookup.Contains(baseType))
					res.AddRange(lookup[baseType]);
			}
			return res;
		}

	}
}
