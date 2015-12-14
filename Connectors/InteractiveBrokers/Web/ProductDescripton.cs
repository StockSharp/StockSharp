#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.InteractiveBrokers.Web.InteractiveBrokers
File: ProductDescripton.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.InteractiveBrokers.Web
{
	using StockSharp.Messages;

	/// <summary>
	/// Detailed information about the instrument.
	/// </summary>
	public class ProductDescripton
	{
		/// <summary>
		/// Name.
		/// </summary>
		public string Name;

		/// <summary>
		/// Code.
		/// </summary>
		public string Symbol;

		/// <summary>
		/// Type.
		/// </summary>
		public SecurityTypes Type;

		/// <summary>
		/// Country.
		/// </summary>
		public string Country;

		/// <summary>
		/// Closing price.
		/// </summary>
		public decimal ClosingPrice;

		/// <summary>
		/// Currency.
		/// </summary>
		public string Currency;

		/// <summary>
		/// Underlying asset.
		/// </summary>
		public string AssetId;

		/// <summary>
		/// Type of shares.
		/// </summary>
		public string StockType;

		/// <summary>
		/// Margin funds.
		/// </summary>
		public string InitialMargin;

		/// <summary>
		/// Margin funds.
		/// </summary>
		public string MaintenanceMargin;

		/// <summary>
		/// Margin funds under short position.
		/// </summary>
		public string ShortMargin;
	}
}