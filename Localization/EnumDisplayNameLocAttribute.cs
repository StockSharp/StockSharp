#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Localization.Localization
File: EnumDisplayNameLocAttribute.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Localization
{
	using System;

	using Ecng.ComponentModel;

	/// <summary>
	/// Specifies the display name for an enum.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	[Obsolete("Use DisplayAttribute instead.")]
	public class EnumDisplayNameLocAttribute : EnumDisplayNameAttribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="EnumDisplayNameLocAttribute"/> class using specified resource id for the display name.
		/// </summary>
		/// <param name="resourceId">String resource id.</param>
		public EnumDisplayNameLocAttribute(string resourceId)
			: base(LocalizedStrings.GetString(resourceId))
		{
		}
	}
}