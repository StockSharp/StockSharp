#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: XMLCommToHTM.DocViewer
File: Strings.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
using System.Collections.Generic;

namespace XMLCommToHTM
{
	public class Strings
	{
		public bool IsRussian=true;
		

		public string this[string nameId] => CurrentNames[nameId];

		public const string
			NestedTypes="NestedTypes", 
			Constructors="Constructors", 
			Properties="Properties", 
			Methods="Methods", 
			Operators="Operators", 
			Fields="Fields",
			Events = "Events",
			ExtentionMethods="ExtentionMethods",

			SuffixDelimeter ="SuffixDelimeter",
			Class = "Class",
			Interface = "Interface",
			Struct ="Struct",
			Enum = "Enum",
			Delegate = "Delegate",
			Namespace = "Namespace",
			Assembly = "Assembly",
			Remarks = "Remarks",
			VersionInfo ="VersionInfo",
			Version = "Version",
			NetRuntimeVersion = "NETRuntimeVersion",
			InheritanceHierarchy ="InheritanceHierarchy",
			Name="Name",
			Description="Description",

			InheritedFrom = "InheritedFrom",
			Overrides = "Overrides",
			StaticMember = "StaticMember",

			Constructor="Constructor", 
			Property="Property", 
			Method="Method", 
			Operator="Operator", 
			Field="Field",
			Event = "Event",

			MethodPub = "MethodPub",
			MethodProt = "MethodProt",
			OperatorPub = "OperatorPub",
			OperatorProt = "OperatorProt",
			ExtentionPub = "ExtentionPub",
			ExtentionProt = "ExtentionProt",
			PropertyPub = "PropertyPub",
			PropertyProt = "PropertyProt",
			FieldPub = "FieldPub",
			FieldProt = "FieldProt",
			EventPub = "EventPub",
			EventProt = "EventProt",

			Parameters = "Parameters",
			TypeParameters = "TypeParameters",
			Type="Type",
			ReturnValue = "ReturnValue",
			OverloadList = "OverloadList"
			;

		private Dictionary<string, string> CurrentNames => IsRussian ? RussianNames : EnglishNames;

		private readonly Dictionary<string, string> EnglishNames = new Dictionary<string, string>
			{ { NestedTypes, "Nested types" },
				{ Constructors, "Constructors" },
				{ Properties, "Properties" },
				{ Methods, "Methods" },
				{ Operators, "Operators" },
				{ Fields, "Fields" },
				{ Events, "Events" },
				{ ExtentionMethods, "Extention methods" },

				{ SuffixDelimeter, " " },
				{ Class, "Class" },
				{ Interface, "Interface" },
				{ Struct, "Struct" },
				{ Enum, "Enum" },
				{ Delegate, "Delegate" },
				{ Class + "_s", "Classes" }, //+"_s" это префикс множественного числа,чтобы автоматически добавлять.
				{ Interface + "_s", "Interfaces" },
				{ Struct + "_s", "Structs" },
				{ Enum + "_s", "Enums" },
				{ Delegate + "_s", "Delegates" },

				{ Namespace, "Namespace" },
				{ Namespace + "_s", "Namespaces" },
				{ Assembly, "Assembly" },
				{ Remarks, "Remarks" },
				{ VersionInfo, "Version information" },
				{ Version, "Version" },
				{ NetRuntimeVersion, ".NET runtime version" },
				{ InheritanceHierarchy, "Inheritance hierarchy" },
				{ Name, "Name" },
				{ Description, "Description" },

				{ InheritedFrom, "Inherited from" },
				{ Overrides, "Overrides" },
				{ StaticMember, "Static member" },

				{ Constructor, "Constructor" },
				{ Property, "Property" },
				{ Method, "Method" },
				{ Operator, "Operator" },
				{ Field, "Field" },
				{ Event, "Event" },

				{ MethodPub, "Public method" }, //для всплывающей подсказки на иконке типа member
				{ MethodProt, "Protected method" },
				{ OperatorPub, "Public operator" },
				{ OperatorProt, "Protected operator"},
				{ ExtentionPub, "Public extention method"},
				{ ExtentionProt, "Protected extention method"},
				{ PropertyPub, "Public property"},
				{ PropertyProt, "Protected property"},
				{ FieldPub, "Public field"},
				{ FieldProt, "Protected field"},
				{ EventPub, "Public event"},
				{ EventProt, "Protected event"},

				{ Parameters, "Parameters" },
				{ Type, "Type" },
				{ TypeParameters, "Type Parameters" },
				{ ReturnValue, "Return Value" },
				{ OverloadList , "Overload List" }
			};

		private readonly Dictionary<string, string> RussianNames = new Dictionary<string, string>
			{
				{ NestedTypes,"Вложенные классы" },
				{ Constructors,"Конструкторы" },
				{ Properties,"Свойства" },
				{ Methods,"Методы" },
				{ Operators,"Операторы" },
				{ Fields,"Поля" },
				{ Events,"События" },
				{ ExtentionMethods,"Методы расширения" },

				{ SuffixDelimeter," - " },
				{ Class,"Класс" },
				{ Interface,"Интерфейс" },
				{ Struct,"Структура" },
				{ Enum , "Перечисление" },
				{ Delegate , "Делегат" },
				{ Class+"_s", "Классы" }, //+"_s" это префикс множественного числа,чтобы автоматически добавлять.
				{ Interface+"_s", "Интерфейсы" },
				{ Struct+"_s", "Структуры" },
				{ Enum+"_s" , "Перечисления" },
				{ Delegate+"_s" , "Делегаты" },



				{ Namespace, "Пространство имен" },
				{ Namespace + "_s", "Пространства имен" },
				{ Assembly, "Сборка" },
				{ Remarks, "Заметки" },
				{ VersionInfo, "Сведения о версии" },
				{ Version, "Версия" },
				{ NetRuntimeVersion, "Версия среды .NET" },
				{ InheritanceHierarchy, "Иерархия наследования" },
				{ Name, "Имя" },
				{ Description, "Описание" },

				{ InheritedFrom, "Унаследовано от" },
				{ Overrides, "Переопределяет" },
				{ StaticMember , "Статический член" },

				{ Constructor, "конструктор" },
				{ Property, "свойство" },
				{ Method, "метод" },
				{ Operator, "оператор" },
				{ Field, "поле" },
				{ Event, "событие" },

				{ MethodPub, "Открытый метод" }, //для всплывающей подсказки на иконке типа member
				{ MethodProt, "Защищенный метод" },
				{ OperatorPub, "Открытый оператор" },
				{ OperatorProt, "Защищенный оператор"},
				{ ExtentionPub, "Открытый метод расширения"},
				{ ExtentionProt, "Защищенный метод расширения"},
				{ PropertyPub, "Открытое свойство"},
				{ PropertyProt, "Защищенное свойство"},
				{ FieldPub, "Открытое поле"},
				{ FieldProt, "Защищенное поле"},
				{ EventPub, "Открытое событие"},
				{ EventProt, "Защищенное сообытие"},

				{ Parameters, "Параметры" },
				{ Type, "Тип"},
				{ TypeParameters, "Параметры типа" },
				{ ReturnValue, "Возвращаемое значение" },
				{ OverloadList, "Список перегрузок" }
			};
	}
}
