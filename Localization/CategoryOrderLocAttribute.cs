#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Localization.Localization
File: CategoryOrderLocAttribute.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Localization
{
	using System;

	//using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// Specifies the order of the category.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple=true)]
	public class CategoryOrderLocAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of the CategoryOrderLocAttribute.
		/// </summary>
		/// <param name="resourceId">String resource id of the category name.</param>
		/// <param name="order">Category order.</param>
		public CategoryOrderLocAttribute(string resourceId, int order)
			//: base(LocalizedStrings.GetString(resourceId), order)
			// TODO
		{
		}
	}
}