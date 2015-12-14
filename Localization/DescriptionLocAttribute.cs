#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Localization.Localization
File: DescriptionLocAttribute.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Localization
{
	using System;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Common;

	/// <summary>
	/// Specifies a description for a property or event.
	/// </summary>
	[AttributeUsage(AttributeTargets.All)]
	public class DescriptionLocAttribute : DescriptionAttribute
	{
		/// <summary>
		/// Initializes a new instance of <see cref="DescriptionLocAttribute"/> class with description.
		/// </summary>
		/// <param name="resourceId">Id of string resource.</param>
		/// <param name="appendDot">Append dot character ('.') at the end of description. Ignore by default.</param>
		public DescriptionLocAttribute(string resourceId, bool appendDot = false)
			: base(GetDescription(resourceId, appendDot))
		{
		}

		/// <summary>
		/// Initializes a new instance of <see cref="DescriptionLocAttribute"/> class with description.
		/// </summary>
		/// <param name="resourceId">Id of string resource.</param>
		/// <param name="arg">Arg for formatted string.</param>
		public DescriptionLocAttribute(string resourceId, string arg)
			: base(LocalizedStrings.GetString(resourceId).Put(LocalizedStrings.GetString(arg)))
		{
		}

		private static string GetDescription(string resourceId, bool appendDot)
		{
			var desc = LocalizedStrings.GetString(resourceId);

			if (appendDot && desc.Last() != '.')
				desc += '.';

			return desc;
		}
	}
}