#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.InteractiveBrokers.InteractiveBrokers
File: ScannerFilter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.InteractiveBrokers
{
	using System;

	/// <summary>
	/// Shares exclusions types.
	/// </summary>
	public enum ScannerFilterStockExcludes
	{
		/// <summary>
		/// Not to exclude anything.
		/// </summary>
		All,

		/// <summary>
		/// To exclude <see cref="Etf"/>.
		/// </summary>
		Stock,

		/// <summary>
		/// Only the Exchange-traded fund.
		/// </summary>
		Etf
	}

	/// <summary>
	/// Filter settings of the scanner starting via <see cref="IBTrader.SubscribeScanner"/>.
	/// </summary>
	public class ScannerFilter
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ScannerFilter"/>.
		/// </summary>
		public ScannerFilter()
		{
		}

		/// <summary>
		/// The number of strings in the query.
		/// </summary>
		public int? RowCount { get; set; }

		/// <summary>
		/// Security type.
		/// </summary>
		public string SecurityType { get; set; }

		/// <summary>
		/// Exchange board.
		/// </summary>
		public string BoardCode { get; set; }

		/// <summary>
		/// </summary>
		public string ScanCode { get; set; }

		/// <summary>
		/// The upper limit of the instrument market price.
		/// </summary>
		public decimal? AbovePrice { get; set; }

		/// <summary>
		/// The lower limit of the instrument market price.
		/// </summary>
		public decimal? BelowPrice { get; set; }

		/// <summary>
		/// The upper limit of the instrument trading volume.
		/// </summary>
		public int? AboveVolume { get; set; }

		/// <summary>
		/// The upper limit of the option trading volume.
		/// </summary>
		public int? AverageOptionVolumeAbove { get; set; }

		/// <summary>
		/// The upper limit of capitalization.
		/// </summary>
		public decimal? MarketCapAbove { get; set; }

		/// <summary>
		/// The lower limit of capitalization.
		/// </summary>
		public decimal? MarketCapBelow { get; set; }

		/// <summary>
		/// The upper limit of the Moody rating.
		/// </summary>
		public string MoodyRatingAbove { get; set; }

		/// <summary>
		/// The lower limit of the Moody rating.
		/// </summary>
		public string MoodyRatingBelow { get; set; }

		/// <summary>
		/// The upper limit of the SP rating.
		/// </summary>
		public string SpRatingAbove { get; set; }

		/// <summary>
		/// The lower limit of the SP rating.
		/// </summary>
		public string SpRatingBelow { get; set; }

		/// <summary>
		/// The upper limit of the instrument maturity date.
		/// </summary>
		public DateTimeOffset? MaturityDateAbove { get; set; }

		/// <summary>
		/// The lower limit of the instrument maturity date.
		/// </summary>
		public DateTimeOffset? MaturityDateBelow { get; set; }

		/// <summary>
		/// The upper limit of the coupon rate.
		/// </summary>
		public decimal? CouponRateAbove { get; set; }

		/// <summary>
		/// The lower limit of the coupon rate.
		/// </summary>
		public decimal? CouponRateBelow { get; set; }

		/// <summary>
		/// To exclude convertible bonds.
		/// </summary>
		public bool ExcludeConvertibleBonds { get; set; }

		/// <summary>
		/// Extended settings. For more details see http://www.interactivebrokers.com/en/software/tws/usersguidebook/technicalanalytics/market_scanner_types.htm.
		/// </summary>
		public string ScannerSettingPairs { get; set; }

		/// <summary>
		/// The shares exclusions type.
		/// </summary>
		public ScannerFilterStockExcludes StockTypeExclude { get; set; }
	}
}