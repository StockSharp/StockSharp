#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.ETrade.ETrade
File: ETradeOrderCondition.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.ETrade
{
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// ETrade condition order type.
	/// </summary>
	public enum ETradeStopTypes
	{
		/// <summary>
		/// The market order is automatically registered after reaching the stop price.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str242Key)]
		StopMarket,

		/// <summary>
		/// The limit order is automatically registered after reaching the stop price.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str1733Key)]
		StopLimit,
	}

	/// <summary>
	/// <see cref="ETrade"/> order condition.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str2264Key, "ETrade")]
	public class ETradeOrderCondition : OrderCondition
	{
		private const string _keyStopType = "StopType";
		private const string _keyStopPrice = "StopPrice";

		/// <summary>
		/// Initializes a new instance of the <see cref="ETradeOrderCondition"/>.
		/// </summary>
		public ETradeOrderCondition()
		{
			StopType = ETradeStopTypes.StopLimit;
			StopPrice = 0;
		}

		/// <summary>
		/// Stop type.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str132Key)]
		[DescriptionLoc(LocalizedStrings.Str1691Key)]
		public ETradeStopTypes StopType
		{
			get { return (ETradeStopTypes)Parameters[_keyStopType]; }
			set { Parameters[_keyStopType] = value; }
		}

		/// <summary>
		/// Stop-price.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.StopPriceKey)]
		[DescriptionLoc(LocalizedStrings.Str1693Key)]
		public decimal StopPrice
		{
			get { return (decimal)Parameters[_keyStopPrice]; }
			set { Parameters[_keyStopPrice] = value; }
		}
	}
}