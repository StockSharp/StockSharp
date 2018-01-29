#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: XMLCommToHTM.DocViewer
File: Navigation.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
using System;

using XMLCommToHTM.DOM;
using XMLCommToHTM.DOM.Internal;

namespace XMLCommToHTM
{
	using System.Collections.Generic;

	using Ecng.Common;

	[Flags]
	public enum MemberIconsEnum
	{
		Static,
		EventPub,
		EventProt,
		MethodExtPub,
		//MethodExtProt,
		MethodProt,
		MethodPub,
		MethodPrv,
		PropertyPub,
		PropertyProt,
		PropertyPrv,
		InterfaceImpl,
		FieldPub,
		FieldProt,
		OperatorPub,
		OperatorProt,
		ClassPub, StructPub, InterfacePub, DelegatePub, EnumPub,
		ClassProt, StructProt, InterfaceProt, DelegateProt, EnumProt,
	};
	public class Navigation
	{
		//private SolutionDom _slnDom;
		public string UrlPrefix;
		public string MsdnUrlPrefix = "http://msdn.microsoft.com/{0}-{0}/library/";

		readonly Dictionary<MemberIconsEnum, string> MemberIcons = new Dictionary<MemberIconsEnum, string>
			{
				{MemberIconsEnum.Static,"doc_static"},
				{MemberIconsEnum.EventPub, "doc_event_public"},
				{MemberIconsEnum.EventProt, "doc_event_protected"},
				{MemberIconsEnum.MethodExtPub,"doc_ext_method_public" },
				//{MemberIconsEnum.MethodExtProt,"doc_ext_method_protected" },
				{MemberIconsEnum.MethodPub, "doc_method_public"},
				{MemberIconsEnum.MethodProt, "doc_method_protected"},
				{MemberIconsEnum.MethodPrv, "doc_method_private"},
				{MemberIconsEnum.PropertyPub, "doc_property_public"},
				{MemberIconsEnum.PropertyProt, "doc_property_protected"},
				{MemberIconsEnum.PropertyPrv, "doc_property_private"},
				{MemberIconsEnum.InterfaceImpl, "doc_interface_impl"},
				{MemberIconsEnum.FieldPub, "doc_public_field"},
				{MemberIconsEnum.FieldProt, "doc_protected_field"},
				{MemberIconsEnum.OperatorPub, "doc_public_operator"},
				{MemberIconsEnum.OperatorProt, "doc_protected_operator"},
				{MemberIconsEnum.ClassPub, "doc_class_public"}, 
				{MemberIconsEnum.ClassProt, "doc_class_protected"}, 
				{MemberIconsEnum.StructPub, "doc_struct_public"},
				{MemberIconsEnum.StructProt, "doc_struct_protected"},
				{MemberIconsEnum.InterfacePub, "doc_interface_public"},
				{MemberIconsEnum.InterfaceProt, "doc_interface_protected"},
				{MemberIconsEnum.DelegatePub, "doc_delegate_public"},
				{MemberIconsEnum.DelegateProt, "doc_delegate_protected"},
				{MemberIconsEnum.EnumPub, "doc_enum_public"},
				{MemberIconsEnum.EnumProt, "doc_enum_protected"},
			};

		public string EmptyImage { get; set; }

		//string MemberIconsPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)+ @"\images\";

		public string GetIconCss(MemberIconsEnum icon)
		{
			return MemberIcons[icon];
		}

		public Tuple<string, bool> GetTypeHref(Type type)
		{
			Type typeForRef = GetTypeForRef(type); //напр. для массива получает тип элементов, ссылка будет на этот тип.
			if (typeForRef == null || typeForRef.FullName == null) //typeForRef.FullName==null когда тип является generic параметром
				return null;
			if (IsMsdn(typeForRef.FullName))
				return Tuple.Create(MsdnUrlPrefix.Put(GenerateHtml.IsRussian ? "ru" : "en") + typeForRef.FullName + ".aspx", false);
			else
				return Tuple.Create(UrlPrefix + type.Namespace.Replace('.','/')+ "/" + Uri.EscapeUriString(TypeUtils.SimpleName(typeForRef)), true);
		}

		public Tuple<string, bool> GetSeeTagHref(string tagHref)
		{
			var isMsdn = IsMsdn(tagHref);

			return Tuple.Create((isMsdn
				? MsdnUrlPrefix.Put(GenerateHtml.IsRussian ? "ru" : "en") + tagHref
				: UrlPrefix + tagHref.Replace('.', '/'))
					, tagHref.StartsWithIgnoreCase("stocksharp"));
		}

		private static bool IsMsdn(string name)
		{
			return name.StartsWithIgnoreCase("system."); //ToDo: усовершенcтвовать
		}

		public string GetMemberHref(MemberDom m)
		{
			var tuple = GetTypeHref(m.Type.Type);
			var ret = tuple.Item1 + "/" + m.Name;
			
			if (m is MethodDom method)
			{
				if (method.OverloadIndex.HasValue)
					ret += "/" + method.OverloadIndex;
			}

			return ret;
		}
		static Type GetTypeForRef(Type type)
		{
			if (type.IsGenericType)
				return type.GetGenericTypeDefinition();
			else if (type.IsGenericParameter)
				return type.DeclaringType;
			else if (type.IsArray)
				return TypeUtils.GetRootElementType(type);
			return type;
		}


		/*
		/// <summary>
		/// 
		/// </summary>
		/// <param name="uri"></param>
		/// <returns>
		/// null - если нет перенаправления, 
		/// string - перенаправляет в интернет(MSDN), строка содержит url
		/// TypeDom - открыть страницу этого класса
		/// MemberDom - открыть страниц этого члена
		/// </returns>
		public object RedirectUri(Uri uri)
		{
			return null;
			if (uri == null || uri.Scheme != "docurl")
				return null;

			if (uri.Host.ToLower() == "type")
			{
				string typeName = Uri.UnescapeDataString(uri.Segments[1]).ToLower();
				if (typeName.StartsWith("system.")) //ToDo: усовершенcтвовать
				{
					return MsdnUrlPrefix + typeName + ".aspx";
				}
				else
				{
					//string[] nams = _slnDom.GetAllTypes().Select(_ => TypeUtils.ToDocString(_.Type).ToLower()).ToArray();
					var type = _slnDom.AllTypes.FirstOrDefault(_ => TypeUtils.ToDocString(_.Type).ToLower() == typeName);
					//if (type != null)
					return type;
				}
			}
			else if (uri.Host.ToLower() == "member")
			{
				int[] indexes = Uri.UnescapeDataString(uri.Segments[1])
					.Split('_')
					.Select(int.Parse)
					.ToArray();
				if (indexes.Length != 2)
					throw new Exception();
				return _slnDom.AllTypes[indexes[0]].AllMembers[indexes[1]];
			}
			throw new Exception("redirection not implemented for :"+uri.Host);

		}
		*/

		
	}


}
