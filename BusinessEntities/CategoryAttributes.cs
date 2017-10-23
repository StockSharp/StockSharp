#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BusinessEntities.BusinessEntities
File: CategoryAttributes.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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

	//class DerivativesCategoryAttribute : CategoryAttribute
	//{
	//	public const string NameKey = LocalizedStrings.Str437Key;

	//	public DerivativesCategoryAttribute()
	//		: base(LocalizedStrings.Str437)
	//	{
	//	}
	//}
}