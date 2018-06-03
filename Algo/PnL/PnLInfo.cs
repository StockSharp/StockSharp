#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.PnL.Algo
File: PnLInfo.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.PnL
{
	using System;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Information on trade, its closed volume and its profitability.
	/// </summary>
	public class PnLInfo
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PnLInfo"/>.
		/// </summary>
		/// <param name="trade">Own trade.</param>
		/// <param name="closedVolume">The volume of position, which was closed by own trade.</param>
		/// <param name="pnL">The profit, realized by this trade.</param>
		public PnLInfo(ExecutionMessage trade, decimal closedVolume, decimal pnL)
		{
			if (closedVolume < 0)
				throw new ArgumentOutOfRangeException(nameof(closedVolume), closedVolume, LocalizedStrings.Str946);

			Trade = trade ?? throw new ArgumentNullException(nameof(trade));
			ClosedVolume = closedVolume;
			PnL = pnL;
		}

		/// <summary>
		/// Own trade.
		/// </summary>
		public ExecutionMessage Trade { get; }

		/// <summary>
		/// The volume of position, which was closed by own trade.
		/// </summary>
		/// <remarks>
		/// For example, in strategy position was 2. The trade for -5 contracts. Closed position 2.
		/// </remarks>
		public decimal ClosedVolume { get; }

		/// <summary>
		/// The profit, realized by this trade.
		/// </summary>
		public decimal PnL { get; }
	}
}