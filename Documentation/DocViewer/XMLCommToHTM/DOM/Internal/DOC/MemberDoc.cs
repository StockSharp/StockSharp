#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: XMLCommToHTM.DOM.Internal.DOC.DocViewer
File: MemberDoc.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
using System;
using System.Reflection;
using System.Xml.Linq;
using Ecng.Common;

namespace XMLCommToHTM.DOM.Internal.DOC
{
	// Парсит член класса(типа) : Field, Property, Method, Event
	public enum MemberType { NotMember, Field, Property, Method, Event }

	public struct Name
	{
		public string
			FullClassName,
			Namespace,
			ClassName,
			MemberName; //Имя члена без имени namespace и класса
	}
	public class MemberDoc
	{
		public XElement DocInfo;
		public Name Name;
		public string
			FullDescription,
			Returns,        //Для методов не заполняется(==null). Только для операторов преобразования типа указывается после закрывающей скобки возвращаемый тип. Например: ...)~Type
			ArgumentsTxt;   //Аргументы в круглых скобках, если они есть.
		public string[] Arguments;
		public int GenericParameterCount;
		public MemberType Type;
		public MemberInfo ReflectionMemberInfo;

		public string ShortDescription
		{
			get
			{
				string ret = Name.MemberName;
				if (ret == "op_Explicit")
				   ret = "explicit operator " + Returns;

				if (!string.IsNullOrEmpty(ArgumentsTxt))
					ret += "(" + ArgumentsTxt + ")";
				else if (Type == MemberType.Method)
					ret += "()";
				return ret;
			}
		}


		//returns: null if not member
		public static MemberDoc ParseMember(XElement e)
		{
			string s = e.Attribute("name").Value;
			var type = GetType(s[0]);
			if (type == MemberType.NotMember)
				return null;
			s = s.Substring(2);
			var ret = new MemberDoc { DocInfo = e, Type = type, FullDescription = s };

			int nameEndIndex = s.Length - 1; //индекс последнего элемента имени
			int par1Index = s.IndexOf('(');
			if (0 <= par1Index)
			{
				int par2Index = s.Length - 1;
				if (s[par2Index] != ')')
				{
					par2Index = s.IndexOf(")~");
					if (par2Index == -1)
						throw new Exception(ErrMsg + ret.FullDescription);
					int retVal = par2Index + 2;
					ret.Returns = s.Substring(retVal, s.Length - retVal);
				}
				ret.ArgumentsTxt = s.Substring(par1Index + 1, par2Index-par1Index-1);//s.Substring(par1Index + 1, s.Length - par1Index - 2);
				nameEndIndex = par1Index - 1;
			}

			ret.Name = ParseUtils.ParseName(s, nameEndIndex);
			ret.Arguments = ParseUtils.SplitArgumentList(ret.ArgumentsTxt);
			ret.FixNames();
			return ret;
		}

		void FixNames()
		{
			Name.MemberName = Name.MemberName.Replace('#', '.');

			int genericIndex = Name.MemberName.IndexOf("``");
			if (genericIndex != -1)
			{
				var s = Name.MemberName.Split(new[]{"``"}, StringSplitOptions.None);
				if(s.Length!=2)
					throw new Exception("Parse error");
				Name.MemberName = s[0];
				GenericParameterCount = int.Parse(s[1]);
			}

			if (Returns != null)
				Returns = FixArgument(Returns);
			if(Arguments!=null)
				for (int i = 0; i < Arguments.Length; i++)
					Arguments[i] = FixArgument(Arguments[i]);
		}

		string FixArgument(string argument)
		{
			argument = argument
				.Replace('@', '&')
				.Remove("0:");
			return argument;
		}

		static string ErrMsg = "Member parse error :";

		static MemberType GetType(char c)
		{
			switch (c)
			{
				case 'F': return MemberType.Field;
				case 'P': return MemberType.Property;
				case 'M': return MemberType.Method;
				case 'E': return MemberType.Event;
				default: return MemberType.NotMember;
			}
		}
	}

}

