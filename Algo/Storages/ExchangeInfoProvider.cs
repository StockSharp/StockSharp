#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Algo
File: ExchangeInfoProvider.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using MoreLinq;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The provider of stocks and trade boards.
	/// </summary>
	public class ExchangeInfoProvider : IExchangeInfoProvider
	{
		private readonly IEntityRegistry _entityRegistry;

		private readonly CachedSynchronizedDictionary<string, ExchangeBoard> _boards = new CachedSynchronizedDictionary<string, ExchangeBoard>(StringComparer.InvariantCultureIgnoreCase);
		private readonly CachedSynchronizedDictionary<string, Exchange> _exchanges = new CachedSynchronizedDictionary<string, Exchange>(StringComparer.InvariantCultureIgnoreCase);

		/// <summary>
		/// Initializes a new instance of the <see cref="ExchangeInfoProvider"/>.
		/// </summary>
		/// <param name="entityRegistry">The storage of trade objects.</param>
		public ExchangeInfoProvider(IEntityRegistry entityRegistry)
		{
			if (entityRegistry == null)
				throw new ArgumentNullException(nameof(entityRegistry));

			ExchangeBoard.EnumerateExchanges().ForEach(exchange => _exchanges.Add(exchange.Name, exchange));
			ExchangeBoard.EnumerateExchangeBoards().ForEach(board => _boards.Add(board.Code, board));

			_entityRegistry = entityRegistry;

			var boardCodes = new HashSet<string>();

			var boardList = _entityRegistry.ExchangeBoards as ExchangeBoardList;
			boardCodes.AddRange(boardList != null ? boardList.GetIds() : _entityRegistry.ExchangeBoards.Select(b => b.Code));

			var boards = Boards.Where(b => !boardCodes.Contains(b.Code)).ToArray();

			if (boards.Length > 0)
			{
				boards
					.Select(b => b.Exchange)
					.Distinct()
					.ForEach(Save);

				boards
					.ForEach(Save);
			}

			_entityRegistry.Exchanges.ForEach(e => _exchanges[e.Name] = e);
			_entityRegistry.ExchangeBoards.ForEach(b => _boards[b.Code] = b);
		}

		/// <summary>
		/// All exchanges.
		/// </summary>
		public IEnumerable<ExchangeBoard> Boards => _boards.CachedValues;

		/// <summary>
		/// All boards.
		/// </summary>
		public IEnumerable<Exchange> Exchanges => _exchanges.CachedValues;

		/// <summary>
		/// Notification about adding a new board.
		/// </summary>
		public event Action<ExchangeBoard> BoardAdded;

		/// <summary>
		/// Notification about adding a new exchange.
		/// </summary>
		public event Action<Exchange> ExchangeAdded;

		/// <summary>
		/// To save the board.
		/// </summary>
		/// <param name="board">Trading board.</param>
		public void Save(ExchangeBoard board)
		{
			if (board == null)
				throw new ArgumentNullException(nameof(board));

			_entityRegistry.ExchangeBoards.Save(board);

			lock (_boards.SyncRoot)
			{
				if (!_boards.TryAdd(board.Code, board))
					return;
			}

			BoardAdded?.Invoke(board);
		}

		/// <summary>
		/// To save the exchange.
		/// </summary>
		/// <param name="exchange">Exchange.</param>
		public void Save(Exchange exchange)
		{
			if (exchange == null)
				throw new ArgumentNullException(nameof(exchange));

			_entityRegistry.Exchanges.Save(exchange);

			lock (_exchanges.SyncRoot)
			{
				if (!_exchanges.TryAdd(exchange.Name, exchange))
					return;
			}

			ExchangeAdded?.Invoke(exchange);
		}

		/// <summary>
		/// To get a board by the code.
		/// </summary>
		/// <param name="code">The board code <see cref="ExchangeBoard.Code"/>.</param>
		/// <returns>Trading board. If the board with the specified code does not exist, then <see langword="null" /> will be returned.</returns>
		public ExchangeBoard GetExchangeBoard(string code)
		{
			if (code.IsEmpty())
				throw new ArgumentNullException(nameof(code));

			return _boards.TryGetValue(code);
		}

		/// <summary>
		/// To get an exchange by the code.
		/// </summary>
		/// <param name="code">The exchange code <see cref="Exchange.Name"/>.</param>
		/// <returns>Exchange. If the exchange with the specified code does not exist, then <see langword="null" /> will be returned.</returns>
		public Exchange GetExchange(string code)
		{
			if (code.IsEmpty())
				throw new ArgumentNullException(nameof(code));

			return _exchanges.TryGetValue(code);
		}
	}
}