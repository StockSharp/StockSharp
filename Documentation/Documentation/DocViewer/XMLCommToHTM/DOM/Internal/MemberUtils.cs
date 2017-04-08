#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: XMLCommToHTM.DOM.Internal.DocViewer
File: MemberUtils.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
using System.Linq;
using System.Reflection;

namespace XMLCommToHTM.DOM.Internal
{
	using System.Xml.Linq;

	public static class MemberUtils
	{
		public static bool IsVisibleMethod(MethodBase m)
		{
			return
				!(m.IsSpecialName &&
				   (
						m.Name.StartsWith("get_") || m.Name.StartsWith("set_") ||
						m.Name.StartsWith("add_") || m.Name.StartsWith("remove_")
				   )
				 );
		}

		public static string GetGenericListSignature(MethodInfo mi)
		{
			var genArgs = mi.GetGenericArguments();
			if (genArgs.Length == 0)
				return "";
			
			return "<" +
				genArgs
				.Select(_ => _.Name)
				.Aggregate((s1, s2) => s1 + ", " + s2)
			+ ">";
		}

		public static string GetParametersShortSignature(ParameterInfo[] pars, int skipCount = 0)
		{
			if (pars.Length < 1 + skipCount)
				return "";
			return "(" +
				pars
				.Skip(skipCount)
				.Select(_ => TypeUtils.ToDisplayString(_.ParameterType, false))
				.Aggregate((s1, s2) => s1 + ", " + s2)
			+ ")";
		}

		public static XElement GetParametersLongSignature(ParameterInfo[] pars, int skipCount = 0)
		{
			if (pars.Length < 1+skipCount)
				return new XElement("span");

			var args = pars
				.Skip(skipCount)
				.SelectMany(p => new[]
				{
					GenerateHtml.BuildTypeUrl(p.ParameterType, false),
					new XElement("span", ","), 
				})
				.ToArray();

			return new XElement("span", new XElement("span", "("), args.Take(args.Length - 1), new XElement("span", ")"));
		}
	}
}