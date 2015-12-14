#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.IQFeed.IQFeed
File: IQFeedLevel1ColumnRegistry.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.IQFeed
{
	using System;

	using Ecng.Collections;

	using StockSharp.Messages;

	/// <summary>
	/// The list of all available <see cref="IQFeedLevel1Column"/>.
	/// </summary>
	public class IQFeedLevel1ColumnRegistry
	{
		internal IQFeedLevel1ColumnRegistry()
		{
			foreach (var field in typeof(IQFeedLevel1ColumnRegistry).GetFields())
			{
				var column = field.GetValue(this) as IQFeedLevel1Column;

				if (column != null)
					_columns.Add(column.Name, column);
			}
		}

		private readonly SynchronizedDictionary<string, IQFeedLevel1Column> _columns = new SynchronizedDictionary<string, IQFeedLevel1Column>(StringComparer.InvariantCultureIgnoreCase);

		private const string _dateFormat = "MM/dd/yyyy";
		private const string _timeFormat = "hh\\:mm\\:ss\\.fff";

		/// <summary>
		/// To get the column by name <see cref="IQFeedLevel1Column.Name"/>.
		/// </summary>
		/// <param name="name">Column name.</param>
		/// <returns>Found column. If the column does not exist then <see langword="null" /> is returned.</returns>
		public IQFeedLevel1Column this[string name]
		{
			get { return _columns.TryGetValue(name); }
		}

		/// <summary>
		/// Security code.
		/// </summary>
		public readonly IQFeedLevel1Column Symbol = new IQFeedLevel1Column("Symbol", typeof(string));

		/// <summary>
		/// Exchange id.
		/// </summary>
		public readonly IQFeedLevel1Column ExchangeId = new IQFeedLevel1Column("Exchange ID", typeof(string));

		/// <summary>
		/// Last trade price.
		/// </summary>
		public readonly IQFeedLevel1Column LastTradePrice = new IQFeedLevel1Column("Last", typeof(decimal)) { Field = Level1Fields.LastTradePrice };

		/// <summary>
		/// Total session volume.
		/// </summary>
		public readonly IQFeedLevel1Column TotalVolume = new IQFeedLevel1Column("Total Volume", typeof(decimal)) { Field = Level1Fields.Volume };

		/// <summary>
		/// Highest session price.
		/// </summary>
		public readonly IQFeedLevel1Column High = new IQFeedLevel1Column("High", typeof(decimal)) { Field = Level1Fields.HighPrice };

		/// <summary>
		/// Lowest session price.
		/// </summary>
		public readonly IQFeedLevel1Column Low = new IQFeedLevel1Column("Low", typeof(decimal)) { Field = Level1Fields.LowPrice };

		/// <summary>
		/// Bid price.
		/// </summary>
		public readonly IQFeedLevel1Column BidPrice = new IQFeedLevel1Column("Bid", typeof(decimal)) { Field = Level1Fields.BestBidPrice };

		/// <summary>
		/// Ask price.
		/// </summary>
		public readonly IQFeedLevel1Column AskPrice = new IQFeedLevel1Column("Ask", typeof(decimal)) { Field = Level1Fields.BestAskPrice };

		/// <summary>
		/// Bid volume.
		/// </summary>
		public readonly IQFeedLevel1Column BidVolume = new IQFeedLevel1Column("Bid Size", typeof(decimal)) { Field = Level1Fields.BestBidVolume };

		/// <summary>
		/// Ask volume.
		/// </summary>
		public readonly IQFeedLevel1Column AskVolume = new IQFeedLevel1Column("Ask Size", typeof(decimal)) { Field = Level1Fields.BestAskVolume };

		/// <summary>
		/// Open interest.
		/// </summary>
		public readonly IQFeedLevel1Column OpenInterest = new IQFeedLevel1Column("Open Interest", typeof(decimal)) { Field = Level1Fields.OpenInterest };

		/// <summary>
		/// Opening price.
		/// </summary>
		public readonly IQFeedLevel1Column Open = new IQFeedLevel1Column("Open", typeof(decimal)) { Field = Level1Fields.OpenPrice };

		/// <summary>
		/// Closing price.
		/// </summary>
		public readonly IQFeedLevel1Column Close = new IQFeedLevel1Column("Close", typeof(decimal)) { Field = Level1Fields.ClosePrice };

		/// <summary>
		/// Estimated value.
		/// </summary>
		public readonly IQFeedLevel1Column Settle = new IQFeedLevel1Column("Settle", typeof(decimal)) { Field = Level1Fields.SettlementPrice };

		/// <summary>
		/// The data delay time in minutes (if not real-time data used).
		/// </summary>
		public readonly IQFeedLevel1Column Delay = new IQFeedLevel1Column("Delay", typeof(int));

		/// <summary>
		/// The flag which means the short sales allowability.
		/// </summary>
		public readonly IQFeedLevel1Column ShortSaleRestrictedCode = new IQFeedLevel1Column("Restricted Code", typeof(string));

		/// <summary>
		/// The value of net yield for mutual funds.
		/// </summary>
		public readonly IQFeedLevel1Column NetAssetValueMutualFonds = new IQFeedLevel1Column("Net Asset Value", typeof(decimal));

		/// <summary>
		/// The average time to delivery.
		/// </summary>
		public readonly IQFeedLevel1Column AverageDaysMaturity = new IQFeedLevel1Column("Average Maturity", typeof(decimal));

		/// <summary>
		/// 7 day yield.
		/// </summary>
		public readonly IQFeedLevel1Column SevenDayYield = new IQFeedLevel1Column("7 Day Yield", typeof(decimal));

		/// <summary>
		/// The value of net yield for FX.
		/// </summary>
		public readonly IQFeedLevel1Column NetAssetValueFx = new IQFeedLevel1Column("Net Asset Value 2", typeof(decimal));

		/// <summary>
		/// The market opening event flag.
		/// </summary>
		public readonly IQFeedLevel1Column MarketOpen = new IQFeedLevel1Column("Market Open", typeof(int));

		/// <summary>
		/// The format of the fractional price.
		/// </summary>
		public readonly IQFeedLevel1Column FractionDisplayCode = new IQFeedLevel1Column("Fraction Display Code", typeof(string));

		/// <summary>
		/// The precision after the decimal point.
		/// </summary>
		public readonly IQFeedLevel1Column DecimalPrecision = new IQFeedLevel1Column("Decimal Precision", typeof(string));

		/// <summary>
		/// The volume of the previous trading session.
		/// </summary>
		public readonly IQFeedLevel1Column PrevDayVolume = new IQFeedLevel1Column("Previous Day Volume", typeof(decimal));

		/// <summary>
		/// Opening range.
		/// </summary>
		public readonly IQFeedLevel1Column OpenRange1 = new IQFeedLevel1Column("Open Range 1", typeof(decimal));

		/// <summary>
		/// Сlosing range.
		/// </summary>
		public readonly IQFeedLevel1Column CloseRange1 = new IQFeedLevel1Column("Close Range 1", typeof(decimal));

		/// <summary>
		/// Opening range.
		/// </summary>
		public readonly IQFeedLevel1Column OpenRange2 = new IQFeedLevel1Column("Open Range 2", typeof(decimal));

		/// <summary>
		/// Сlosing range.
		/// </summary>
		public readonly IQFeedLevel1Column CloseRange2 = new IQFeedLevel1Column("Close Range 2", typeof(decimal));

		/// <summary>
		/// The number of trades per session.
		/// </summary>
		public readonly IQFeedLevel1Column TradeCount = new IQFeedLevel1Column("Number of Trades Today", typeof(int)) { Field = Level1Fields.TradesCount };

		/// <summary>
		/// VWAP.
		/// </summary>
		public readonly IQFeedLevel1Column VWAP = new IQFeedLevel1Column("VWAP", typeof(decimal)) { Field = Level1Fields.VWAP };

		/// <summary>
		/// Last trade ID.
		/// </summary>
		public readonly IQFeedLevel1Column LastTradeId = new IQFeedLevel1Column("TickID", typeof(long)) { Field = Level1Fields.LastTradeId };

		/// <summary>
		/// Indicator code.
		/// </summary>
		public readonly IQFeedLevel1Column FinancialStatusIndicator = new IQFeedLevel1Column("Financial Status Indicator", typeof(string));

		/// <summary>
		/// Settlement date.
		/// </summary>
		public readonly IQFeedLevel1Column SettlementDate = new IQFeedLevel1Column("Settlement Date", typeof(DateTime), _dateFormat);

		/// <summary>
		/// The bid market identifier.
		/// </summary>
		public readonly IQFeedLevel1Column BidMarket = new IQFeedLevel1Column("Bid Market Center", typeof(int));

		/// <summary>
		/// The offer market identifier.
		/// </summary>
		public readonly IQFeedLevel1Column AskMarket = new IQFeedLevel1Column("Ask Market Center", typeof(int));

		/// <summary>
		/// Possible regions.
		/// </summary>
		public readonly IQFeedLevel1Column AvailableRegions = new IQFeedLevel1Column("Available Regions", typeof(string));

		/// <summary>
		/// Last trade volume.
		/// </summary>
		public readonly IQFeedLevel1Column LastTradeVolume = new IQFeedLevel1Column("Last Size", typeof(decimal)) { Field = Level1Fields.LastTradeVolume };

		/// <summary>
		/// Time of last trade.
		/// </summary>
		public readonly IQFeedLevel1Column LastTradeTime = new IQFeedLevel1Column("Last TimeMS", typeof(TimeSpan), _timeFormat) { Field = Level1Fields.LastTradeTime };

		/// <summary>
		/// The last trade market identifier.
		/// </summary>
		public readonly IQFeedLevel1Column LastTradeMarket = new IQFeedLevel1Column("Last Market Center", typeof(int));

		/// <summary>
		/// The most frequent trade price.
		/// </summary>
		public readonly IQFeedLevel1Column MostRecentTradePrice = new IQFeedLevel1Column("Most Recent Trade", typeof(decimal));

		/// <summary>
		/// The most frequent trade volume.
		/// </summary>
		public readonly IQFeedLevel1Column MostRecentTradeVolume = new IQFeedLevel1Column("Most Recent Trade Size", typeof(decimal));

		/// <summary>
		/// The most frequent trade time.
		/// </summary>
		public readonly IQFeedLevel1Column MostRecentTradeTime = new IQFeedLevel1Column("Most Recent Trade TimeMS", typeof(TimeSpan), _timeFormat);

		/// <summary>
		/// The most frequent trade condition.
		/// </summary>
		public readonly IQFeedLevel1Column MostRecentTradeConditions = new IQFeedLevel1Column("Most Recent Trade Conditions", typeof(string));

		/// <summary>
		/// The market identifier of the most frequent trade .
		/// </summary>
		public readonly IQFeedLevel1Column MostRecentTradeMarket = new IQFeedLevel1Column("Most Recent Trade Market Center", typeof(int));

		/// <summary>
		/// The price of the last extended trade.
		/// </summary>
		public readonly IQFeedLevel1Column ExtendedTradePrice = new IQFeedLevel1Column("Extended Trade", typeof(decimal));

		/// <summary>
		/// The volume of the last extended trade.
		/// </summary>
		public readonly IQFeedLevel1Column ExtendedTradeVolume = new IQFeedLevel1Column("Extended Trade Size", typeof(decimal));

		/// <summary>
		/// The time of the last extended trade.
		/// </summary>
		public readonly IQFeedLevel1Column ExtendedTradeTime = new IQFeedLevel1Column("Extended Trade TimeMS", typeof(TimeSpan), _timeFormat);

		/// <summary>
		/// The market identifier of the last extended trade.
		/// </summary>
		public readonly IQFeedLevel1Column ExtendedTradeMarket = new IQFeedLevel1Column("Extended Trade Market Center", typeof(int));

		/// <summary>
		/// Content codes.
		/// </summary>
		public readonly IQFeedLevel1Column MessageContents = new IQFeedLevel1Column("Message Contents", typeof(string));

		/// <summary>
		/// Ask time.
		/// </summary>
		public readonly IQFeedLevel1Column AskTime = new IQFeedLevel1Column("Ask TimeMS", typeof(TimeSpan), _timeFormat) { Field = Level1Fields.BestAskTime };

		/// <summary>
		/// Bid time.
		/// </summary>
		public readonly IQFeedLevel1Column BidTime = new IQFeedLevel1Column("Bid TimeMS", typeof(TimeSpan), _timeFormat) { Field = Level1Fields.BestBidTime };

		/// <summary>
		/// The time of the last date trade.
		/// </summary>
		public readonly IQFeedLevel1Column LastDate = new IQFeedLevel1Column("Last Date", typeof(DateTime), _dateFormat);
		
		/// <summary>
		/// The date of the last extended trade.
		/// </summary>
		public readonly IQFeedLevel1Column LastExtendedTradeDate = new IQFeedLevel1Column("Extended Trade Date", typeof(DateTime), _dateFormat);
		
		/// <summary>
		/// The most frequent trade date.
		/// </summary>
		public readonly IQFeedLevel1Column MostRecentTradeDate = new IQFeedLevel1Column("Most Recent Trade Date", typeof(DateTime), _dateFormat);
	}
}