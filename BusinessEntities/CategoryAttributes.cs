namespace StockSharp.BusinessEntities
{
	using System.ComponentModel;
	using StockSharp.Localization;

	class MainCategoryAttribute : CategoryAttribute
	{
		public const string NameKey = LocalizedStrings.GeneralKey;

		public MainCategoryAttribute()
			: base(LocalizedStrings.General)
		{
		}
	}

	class StatisticsCategoryAttribute : CategoryAttribute
	{
		public const string NameKey = LocalizedStrings.Str436Key;

		public StatisticsCategoryAttribute()
			: base(LocalizedStrings.Str436)
		{
		}
	}

	class DerivativesCategoryAttribute : CategoryAttribute
	{
		public const string NameKey = LocalizedStrings.Str437Key;

		public DerivativesCategoryAttribute()
			: base(LocalizedStrings.Str437)
		{
		}
	}
}