#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: XMLCommToHTM.DOM.Internal.DocViewer
File: ParseUtils.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
using System.Collections.Generic;
using XMLCommToHTM.DOM.Internal.DOC;

namespace XMLCommToHTM.DOM.Internal
{
	public static class ParseUtils
	{
		/// <summary>
		///  Разбирает имя типа : namespace1.namespace2.class.member на составляющие.
		///  Часть имен может отсутствовать.
		/// </summary>
		/// <param name="s"> Строка содержащая имя</param>
		/// <param name="nameEndIndex"> Индекс последнего символа в имени</param>
		/// <returns></returns>
		public static Name ParseName(string s, int nameEndIndex)
		{
			var ret = new Name();

			if (nameEndIndex < 0 || string.IsNullOrEmpty(s) /*|| s.Length<=nameEndIndex*/)
				return ret;
			int nameStart = s.LastIndexOf('.', nameEndIndex) + 1;
			ret.MemberName = s.Substring(nameStart, nameEndIndex - nameStart + 1);

			int classEnd = nameStart - 2;
			if (classEnd < 0)
				return ret;

			ret.FullClassName = s.Substring(0, classEnd + 1);
			int classStart = s.LastIndexOf('.', classEnd) + 1;
			ret.ClassName = s.Substring(classStart, classEnd - classStart + 1);

			int nsEndIndex = classStart - 2;
			if (nsEndIndex < 0)
				return ret;
			ret.Namespace = s.Substring(0, nsEndIndex + 1);
			return ret;
		}

		/// <summary>
		/// Разделяет список аргументов функции или проперти, на отдельные аргументы.
		/// В списке аргументов может быть такой элемент System.Int32[0:,0:] , поэтому нельзя просто делать string.Split
		/// Также может быть такое имя типа аргумента "X{`0,`1}" , здесь тип X имеет два generic параметра
		/// </summary>
		/// <param name="arguments"> Строка списка аргументов</param>
		/// <returns> Разделенные аргументы</returns>
		public static string[] SplitArgumentList(string arguments)
		{
			if (string.IsNullOrEmpty(arguments))
				return null;
			int brDepth = 0; //глубина вложенности квадратных скобок
			int brDepth2 = 0; //глубина вложенности фигурных скобок
			int start = 0;
			var ret = new List<string>();
			for (int i = 0; i < arguments.Length; i++)
			{
				switch (arguments[i])
				{   
					case '[': brDepth++;    break;
					case ']': brDepth--;    break;
					case '{': brDepth2++;   break;
					case '}': brDepth2--;   break;
					case ',': 
						if (brDepth == 0 && brDepth2 == 0)
						{
							ret.Add(arguments.Substring(start, i - start));
							start = i + 1;
						}
						break;
				}
			}
			ret.Add(arguments.Substring(start, arguments.Length - start));
			return ret.ToArray();
		}

	}
}
