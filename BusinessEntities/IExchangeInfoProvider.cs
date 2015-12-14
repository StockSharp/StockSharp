#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BusinessEntities.BusinessEntities
File: IExchangeInfoProvider.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Interface describing exchanges and trading boards provider.
	/// </summary>
	public interface IExchangeInfoProvider
	{
		/// <summary>
		/// All exchanges.
		/// </summary>
		IEnumerable<ExchangeBoard> Boards { get; }

		/// <summary>
		/// All boards.
		/// </summary>
		IEnumerable<Exchange> Exchanges { get; }

		/// <summary>
		/// To get a board by the code.
		/// </summary>
		/// <param name="code">The board code <see cref="ExchangeBoard.Code"/>.</param>
		/// <returns>Trading board. If the board with the specified code does not exist, then <see langword="null" /> will be returned.</returns>
		ExchangeBoard GetExchangeBoard(string code);

		/// <summary>
		/// To get an exchange by the code.
		/// </summary>
		/// <param name="code">The exchange code <see cref="Exchange.Name"/>.</param>
		/// <returns>Exchange. If the exchange with the specified code does not exist, then <see langword="null" /> will be returned.</returns>
		Exchange GetExchange(string code);

		/// <summary>
		/// To save the board.
		/// </summary>
		/// <param name="board">Trading board.</param>
		void Save(ExchangeBoard board);

		/// <summary>
		/// To save the exchange.
		/// </summary>
		/// <param name="exchange">Exchange.</param>
		void Save(Exchange exchange);

		/// <summary>
		/// Notification about adding a new board.
		/// </summary>
		event Action<ExchangeBoard> BoardAdded;

		/// <summary>
		/// Notification about adding a new exchange.
		/// </summary>
		event Action<Exchange> ExchangeAdded;
	}
}