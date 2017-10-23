#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: XMLCommToHTM.DOM.DocViewer
File: ParameterDom.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
using System;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace XMLCommToHTM.DOM
{
	public abstract class ParameterBaseDom
	{
		public XElement DocInfo;
		public abstract string Name { get; }
		public abstract Type Type { get; }	

		
	}

	public class ParameterDictionary
	{
		private readonly Tuple<string, XElement>[] _params;
		public ParameterDictionary(XElement doc, string tagName)
		{
			_params=doc    //Dictionary создавать не имеет смысла, т.к. параметров мало.
				.Elements(tagName)
				.Where(_ => _.Attribute("name") != null)
				.Select(_ => Tuple.Create(_.Attribute("name").Value, _))
				.ToArray();
		}
		public XElement this[string name]
		{
			get
			{
				foreach (var entry in _params)
					if (name == entry.Item1)
						return entry.Item2;
				return null;
			}
		}
	}

	public class ParameterDom : ParameterBaseDom
	{
		public ParameterDom(ParameterInfo parameterInfo)
		{
			Parameter = parameterInfo;
		}

		readonly ParameterInfo Parameter;

		public override string Name => Parameter.Name;
		public override Type Type => Parameter.ParameterType;

		//public string TypeName
		//{
		//	get { return TypeUtils.ToDisplayString(Parameter.ParameterType, true); }
		//}

		public static ParameterDom[] BuildParameters(ParameterInfo[] piAr, XElement parentDoc)
		{
			if(piAr==null || piAr.Length==0)
				return new ParameterDom[0];
			if(parentDoc==null)
				return piAr.Select(_ => new ParameterDom(_)).ToArray();

			var pd = new ParameterDictionary(parentDoc, "param");
			return piAr
				.Select( _ => new ParameterDom(_){DocInfo = pd[_.Name]})
				.ToArray();
		}
	}

	public class GenericParameterDom : ParameterBaseDom
	{
		public GenericParameterDom(string name)
		{
			Name = name;
		}
		public override string Name { get; }

		public override Type Type => null;

		public static GenericParameterDom[] BuildTypeGenericParameters(Type type, XElement typeDoc)
		{
			if (!type.IsGenericType)
				return null;
			type = type.GetGenericTypeDefinition();
			if (type == null)
				return null;
			return BuildGenericParameters(type.GetGenericArguments(),typeDoc);
		}
		public static GenericParameterDom[] BuildMethodGenericParameters(MethodInfo mi, XElement typeDoc)
		{
			if (!mi.IsGenericMethod)
				return null;
			return BuildGenericParameters(mi.GetGenericMethodDefinition().GetGenericArguments(), typeDoc);
		}
		public static GenericParameterDom[] BuildGenericParameters(Type[] genericArgs, XElement typeDoc)
		{
			if (genericArgs.Length == 0)
				return null;

			if (typeDoc == null)
				return genericArgs.Select(_ => new GenericParameterDom(_.Name)).ToArray();

			var pd = new ParameterDictionary(typeDoc, "typeparam");
			return genericArgs
				.Select(_ => new GenericParameterDom(_.Name) { DocInfo = pd[_.Name] })
				.ToArray();
		}
		

	}
}
