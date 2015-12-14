#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.InteractiveBrokers.Web.InteractiveBrokers
File: ProductBoard.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.InteractiveBrokers.Web
{
	/// <summary>
	/// Information about the instrument on the specific board.
	/// </summary>
	public class ProductBoard
	{
		/// <summary>
		/// List of exchanges.
		/// </summary>
		public string Markets;

		/// <summary>
		/// Unique ID.
		/// </summary>
		public string Id;

		/// <summary>
		/// Name.
		/// </summary>
		public string Name;

		/// <summary>
		/// Class.
		/// </summary>
		public string Class;

		/// <summary>
		/// Deliverable.
		/// </summary>
		public string SettlementMethod;

		/// <summary>
		/// Exchange web site.
		/// </summary>
		public string ExchangeWebsite;

		/// <summary>
		/// Business hours.
		/// </summary>
		public string[] TradingHours;

		/// <summary>
		/// The price range.
		/// </summary>
		public string PriceRange1;

		/// <summary>
		/// The price range.
		/// </summary>
		public string PriceRange2;

		/// <summary>
		/// The price range.
		/// </summary>
		public string PriceRange3;

		/// <summary>
		/// The volume range.
		/// </summary>
		public decimal VolumeRange1;

		/// <summary>
		/// The volume range.
		/// </summary>
		public decimal VolumeRange2;

		/// <summary>
		/// The volume range.
		/// </summary>
		public decimal VolumeRange3;
	}
}