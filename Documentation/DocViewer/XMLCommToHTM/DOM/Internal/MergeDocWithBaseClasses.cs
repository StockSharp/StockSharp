#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: XMLCommToHTM.DOM.Internal.DocViewer
File: MergeDocWithBaseClasses.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace XMLCommToHTM.DOM.Internal
{
	public static class MergeDocWithBaseClasses
	{
		private class TypeEntry
		{
			public readonly List<TypeDom> MostDerivedToBase=new List<TypeDom>(); //первый элемент сам класс, дальше базовые
		}
		public static void Merge(TypeDom[] allTypes)
		{
			//словарь типов, в значении список первый элемент которого TypeDom того же типа, следующие элементы - базовые классы
			//по мере обработки словаря эти базовые классы если присутствуют в списке, то должны отсутствовать в словаре.
			//Т.е. во всех списках словаря, конкретный TypeDom присутствует в одном экземпляре.
			Dictionary<Type,List<TypeDom>> dict = allTypes.ToDictionary(_ => _.Type, t => new List<TypeDom> {t});

			foreach (var typeDom in allTypes)
			{
				// = dict[typeDom.Type];
				if (!dict.TryGetValue(typeDom.Type, out var curEntry))
					continue;

				foreach (Type baseType in typeDom.Type.GetBaseTypes())
				{
					//Извлечение и удаление из словаря, и добавление извлеченного списка к curEntry
					if (dict.TryGetValue(baseType, out var baseEntry))
					{
						dict.Remove(baseType);
						curEntry.AddRange(baseEntry);
					}
				}
			}
			foreach (List<TypeDom> lst in dict.Values)
			{
				if (1 < lst.Count)
					MergeBaseToDerived(lst.AsEnumerable().Reverse());
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="baseToDerived">
		/// Цепочка наследования - первый элемент массива базовый класс, последний самый derived.
		/// В цепочке наследования пропущены классы, которые не представлены в документации.
		/// </param>
		static void MergeBaseToDerived(IEnumerable<TypeDom> baseToDerived)
		{
			var curDocs = new Dictionary<string, XElement>();
			foreach (var typeDom in baseToDerived)
			{
				foreach (var memberDom in typeDom.AllMembers.Where(_ => !(_ is ConstructorDom)))
				{
					string name = memberDom.ToString();
					if (memberDom.DocInfo == null)
						curDocs.TryGetValue(name, out memberDom.DocInfo); //заполнение из базового класса
					else
						curDocs[name] = memberDom.DocInfo; //сохранение для производных классов
				}
			}
		}
	}
}
