#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: XMLCommToHTM.DOM.DocViewer
File: ConstructorDom.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
using System.Reflection;
using System.Xml.Linq;
using XMLCommToHTM.DOM.Internal;

namespace XMLCommToHTM.DOM
{
	public class ConstructorDom : MemberDom
	{
		private ConstructorInfo _ci;
		public ConstructorDom(ConstructorInfo ci, XElement doc) : base(ci, doc)
		{
			_ci = ci;
			Params = ParameterDom.BuildParameters(ci.GetParameters(), doc);
		}
		public override string ShortSignature
		{
			get
			{
				string ret= TypeUtils.SimpleName(_ci.DeclaringType);
				ret += GetParametersShortSignature();
				return ret;
			}
		}

		public override string GetParametersShortSignature()
		{
			return MemberUtils.GetParametersShortSignature(_ci.GetParameters());
		}
		public override bool IsPublic { get { return _ci.IsPublic; } }
		public override bool IsPrivateOrInternal { get { return _ci.IsPrivate || _ci.IsAssembly; } }
		public override bool IsStatic { get { return _ci.IsStatic; } }

		public override System.Type MemberType
		{
			get { return null; }
		}
	}
}