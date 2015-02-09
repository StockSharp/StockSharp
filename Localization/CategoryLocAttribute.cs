namespace StockSharp.Localization
{
	using System;
	using System.ComponentModel;

	/// <summary>
	/// Specifies the name of the category in which to group the property or event when displayed in a System.Windows.Forms.PropertyGrid control set to Categorized mode.
	/// </summary>
	[AttributeUsageAttribute(AttributeTargets.All)]
	public class CategoryLocAttribute : CategoryAttribute
	{
		/// <summary>
		/// Initializes a new instance of the CategoryLocAttribute class using the specified resource id for category name.
		/// </summary>
		/// <param name="resourceId">String resource id to use for category name.</param>
		public CategoryLocAttribute(string resourceId)
			: base(LocalizedStrings.GetString(resourceId))
		{
		}
	}
}
