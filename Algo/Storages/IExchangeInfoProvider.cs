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
namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Collections;

	using MoreLinq;

	using StockSharp.BusinessEntities;

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

		/// <summary>
		/// Notification about removing the existing board.
		/// </summary>
		event Action<ExchangeBoard> BoardRemoved;

		/// <summary>
		/// Notification about removing the existing exchange.
		/// </summary>
		event Action<Exchange> ExchangeRemoved;

		/// <summary>
		/// Delete exchange.
		/// </summary>
		/// <param name="exchange">Exchange.</param>
		void Delete(Exchange exchange);

		/// <summary>
		/// Delete exchange board.
		/// </summary>
		/// <param name="board">Exchange board.</param>
		void Delete(ExchangeBoard board);
	}

	/// <summary>
	/// The in memory provider of stocks and trade boards.
	/// </summary>
	public class InMemoryExchangeInfoProvider : IExchangeInfoProvider
	{
		private readonly CachedSynchronizedDictionary<string, ExchangeBoard> _boards = new CachedSynchronizedDictionary<string, ExchangeBoard>(StringComparer.InvariantCultureIgnoreCase);
		private readonly CachedSynchronizedDictionary<string, Exchange> _exchanges = new CachedSynchronizedDictionary<string, Exchange>(StringComparer.InvariantCultureIgnoreCase);

		/// <summary>
		/// Initializes a new instance of the <see cref="InMemoryExchangeInfoProvider"/>.
		/// </summary>
		public InMemoryExchangeInfoProvider()
		{
			ExchangeBoard.EnumerateExchanges().ForEach(b => _exchanges[b.Name] = b);
			ExchangeBoard.EnumerateExchangeBoards().ForEach(b => _boards[b.Code] = b);
		}

		/// <inheritdoc />
		public IEnumerable<ExchangeBoard> Boards => _boards.CachedValues;

		/// <inheritdoc />
		public IEnumerable<Exchange> Exchanges => _exchanges.CachedValues;

		/// <inheritdoc />
		public ExchangeBoard GetExchangeBoard(string code)
		{
			if (code.IsEmpty())
				throw new ArgumentNullException(nameof(code));

			return _boards.TryGetValue(code);
		}

		/// <inheritdoc />
		public Exchange GetExchange(string code)
		{
			if (code.IsEmpty())
				throw new ArgumentNullException(nameof(code));

			if (code.CompareIgnoreCase("RTS"))
				code = "FORTS";

			return _exchanges.TryGetValue(code);
		}

		/// <inheritdoc />
		public virtual void Save(ExchangeBoard board)
		{
			if (board == null)
				throw new ArgumentNullException(nameof(board));

			lock (_boards.SyncRoot)
			{
				var oldBoard = _boards.TryGetValue(board.Code);

				if (ReferenceEquals(oldBoard, board))
					return;

				_boards[board.Code] = board;
			}

			BoardAdded?.Invoke(board);
		}

		/// <inheritdoc />
		public virtual void Save(Exchange exchange)
		{
			if (exchange == null)
				throw new ArgumentNullException(nameof(exchange));

			lock (_exchanges.SyncRoot)
			{
				var oldExchange = _exchanges.TryGetValue(exchange.Name);

				if (ReferenceEquals(oldExchange, exchange))
					return;

				_exchanges[exchange.Name] = exchange;
			}

			ExchangeAdded?.Invoke(exchange);
		}

		/// <inheritdoc />
		public event Action<ExchangeBoard> BoardAdded;

		/// <inheritdoc />
		public event Action<Exchange> ExchangeAdded;

		/// <inheritdoc />
		public event Action<ExchangeBoard> BoardRemoved;

		/// <inheritdoc />
		public event Action<Exchange> ExchangeRemoved;

		/// <inheritdoc />
		public virtual void Delete(Exchange exchange)
		{
			if (exchange == null)
				throw new ArgumentNullException(nameof(exchange));

			_exchanges.Remove(exchange.Name);
			ExchangeRemoved?.Invoke(exchange);
		}

		/// <inheritdoc />
		public virtual void Delete(ExchangeBoard board)
		{
			if (board == null)
				throw new ArgumentNullException(nameof(board));

			_boards.Remove(board.Code);
			BoardRemoved?.Invoke(board);
		}
	}

	/// <summary>
	/// The storage based provider of stocks and trade boards.
	/// </summary>
	public class StorageExchangeInfoProvider : InMemoryExchangeInfoProvider
	{
		private readonly IEntityRegistry _entityRegistry;

		/// <summary>
		/// Initializes a new instance of the <see cref="StorageExchangeInfoProvider"/>.
		/// </summary>
		/// <param name="entityRegistry">The storage of trade objects.</param>
		public StorageExchangeInfoProvider(IEntityRegistry entityRegistry)
		{
			_entityRegistry = entityRegistry ?? throw new ArgumentNullException(nameof(entityRegistry));

			var boardCodes = new HashSet<string>();

			boardCodes.AddRange(_entityRegistry.ExchangeBoards is ExchangeBoardList boardList
				? boardList.GetIds() : _entityRegistry.ExchangeBoards.Select(b => b.Code));

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

			_entityRegistry.Exchanges.ForEach(e => base.Save(e));
			_entityRegistry.ExchangeBoards.ForEach(b => base.Save(b));
		}

		/// <inheritdoc />
		public override void Save(ExchangeBoard board)
		{
			if (board == null)
				throw new ArgumentNullException(nameof(board));

			_entityRegistry.ExchangeBoards.Save(board);

			base.Save(board);
		}

		/// <inheritdoc />
		public override void Save(Exchange exchange)
		{
			if (exchange == null)
				throw new ArgumentNullException(nameof(exchange));

			_entityRegistry.Exchanges.Save(exchange);

			base.Save(exchange);
		}
	}
}