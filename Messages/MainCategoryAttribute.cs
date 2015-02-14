namespace StockSharp.Messages
{
	using System.ComponentModel;
	using StockSharp.Localization;

	class MainCategoryAttribute : CategoryAttribute
	{
		public MainCategoryAttribute()
			: base(LocalizedStrings.General)
		{
		}
	}
}