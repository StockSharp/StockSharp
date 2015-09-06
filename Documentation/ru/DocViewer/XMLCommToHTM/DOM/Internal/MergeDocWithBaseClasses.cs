using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
				List<TypeDom> curEntry; // = dict[typeDom.Type];
				if(!dict.TryGetValue(typeDom.Type, out curEntry))
					continue;

				foreach (Type baseType in TypeUtils.GetBaseTypes(typeDom.Type))
				{
					//Извлечение и удаление из словаря, и добавление извлеченного списка к curEntry
					List<TypeDom> baseEntry;
					if (dict.TryGetValue(baseType, out baseEntry))
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
