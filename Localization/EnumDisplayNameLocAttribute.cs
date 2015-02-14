namespace StockSharp.Localization
{
	using System;

	using Ecng.ComponentModel;

	/// <summary>
	/// Specifies the display name for an enum.
	/// </summary>
	[AttributeUsageAttribute(AttributeTargets.Field)]
	public class EnumDisplayNameLocAttribute : EnumDisplayNameAttribute
	{
		/// <summary>
		/// Initializes a new instance of the EnumDisplayNameLocAttribute class using specified resource id for the display name.
		/// </summary>
		/// <param name="resourceId">String resource id.</param>
		public EnumDisplayNameLocAttribute(string resourceId)
			: base(LocalizedStrings.GetString(resourceId))
		{
		}
	}
}
