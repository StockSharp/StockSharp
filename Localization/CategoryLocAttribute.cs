#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Localization.Localization
File: CategoryLocAttribute.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Localization
{
	using System;
	using System.ComponentModel;

	/// <summary>
	/// Specifies the name of the category in which to group the property or event when displayed in a PropertyGrid control set to Categorized mode.
	/// </summary>
	[AttributeUsage(AttributeTargets.All)]
	public class CategoryLocAttribute : CategoryAttribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CategoryLocAttribute"/> class using the specified resource id for category name.
		/// </summary>
		/// <param name="resourceId">String resource id to use for category name.</param>
		public CategoryLocAttribute(string resourceId)
			: base(LocalizedStrings.GetString(resourceId))
		{
		}
	}
}