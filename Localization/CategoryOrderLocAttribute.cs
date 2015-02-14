namespace StockSharp.Localization
{
	using System;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// Specifies the order of the category.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple=true)]
	public class CategoryOrderLocAttribute : CategoryOrderAttribute
	{
		/// <summary>
		/// Initializes a new instance of the CategoryOrderLocAttribute.
		/// </summary>
		/// <param name="resourceId">String resource id of the category name.</param>
		/// <param name="order">Category order.</param>
		public CategoryOrderLocAttribute(string resourceId, int order)
			: base(LocalizedStrings.GetString(resourceId), order)
		{
		}
	}
}
