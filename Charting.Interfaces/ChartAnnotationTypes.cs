namespace StockSharp.Charting
{
	using System;
	using System.ComponentModel.DataAnnotations;

	using StockSharp.Localization;

	/// <summary>
	/// The annotations types.
	/// </summary>
	[Serializable]
	public enum ChartAnnotationTypes
	{
		/// <summary>
		/// None.
		/// </summary>
		None,

		/// <summary>
		/// Line.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1898Key)]
		LineAnnotation,

		/// <summary>
		/// Pointer.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1899Key)]
		LineArrowAnnotation,

		/// <summary>
		/// Text.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str217Key)]
		TextAnnotation,

		/// <summary>
		/// Area.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AreaKey)]
		BoxAnnotation,

		/// <summary>
		/// Horizontal line.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1901Key)]
		HorizontalLineAnnotation,

		/// <summary>
		/// Vertical line.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1902Key)]
		VerticalLineAnnotation,

		/// <summary>
		/// Ruler.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1902Key)]
		RulerAnnotation,
	}
}