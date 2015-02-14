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
	/// Провайдер бирж и торговых площадок.
	/// </summary>
	public class ExchangeInfoProvider : IExchangeInfoProvider
	{
		private readonly IEntityRegistry _entityRegistry;

		private readonly CachedSynchronizedDictionary<string, ExchangeBoard> _boards = new CachedSynchronizedDictionary<string, ExchangeBoard>(StringComparer.InvariantCultureIgnoreCase);
		private readonly CachedSynchronizedDictionary<string, Exchange> _exchanges = new CachedSynchronizedDictionary<string, Exchange>(StringComparer.InvariantCultureIgnoreCase);

		/// <summary>
		/// Создать <see cref="ExchangeInfoProvider"/>.
		/// </summary>
		/// <param name="entityRegistry">Хранилище торговых объектов.</param>
		public ExchangeInfoProvider(IEntityRegistry entityRegistry)
		{
			if (entityRegistry == null)
				throw new ArgumentNullException("entityRegistry");

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
		/// Все биржи.
		/// </summary>
		public IEnumerable<ExchangeBoard> Boards
		{
			get { return _boards.CachedValues; }
		}

		/// <summary>
		/// Все площадки.
		/// </summary>
		public IEnumerable<Exchange> Exchanges
		{
			get { return _exchanges.CachedValues; }
		}

		/// <summary>
		/// Оповещение о добавлении новой площадки.
		/// </summary>
		public event Action<ExchangeBoard> BoardAdded;

		/// <summary>
		/// Оповещение о добавлении новой биржи.
		/// </summary>
		public event Action<Exchange> ExchangeAdded;

		/// <summary>
		/// Сохранить площадку.
		/// </summary>
		/// <param name="board">Торговая площадка.</param>
		public void Save(ExchangeBoard board)
		{
			if (board == null)
				throw new ArgumentNullException("board");

			_entityRegistry.ExchangeBoards.Save(board);

			lock (_boards.SyncRoot)
			{
				if (!_boards.TryAdd(board.Code, board))
					return;
			}

			BoardAdded.SafeInvoke(board);
		}

		/// <summary>
		/// Сохранить биржу.
		/// </summary>
		/// <param name="exchange">Биржа.</param>
		public void Save(Exchange exchange)
		{
			if (exchange == null)
				throw new ArgumentNullException("exchange");

			_entityRegistry.Exchanges.Save(exchange);

			lock (_exchanges.SyncRoot)
			{
				if (!_exchanges.TryAdd(exchange.Name, exchange))
					return;
			}

			ExchangeAdded.SafeInvoke(exchange);
		}

		/// <summary>
		/// Получить площадку по коду.
		/// </summary>
		/// <param name="code">Код площадки <see cref="ExchangeBoard.Code"/>.</param>
		/// <returns>Торговая площадка. Если площадка с заданным кодом не существует, то будет возвращено <see langword="null"/>.</returns>
		public ExchangeBoard GetExchangeBoard(string code)
		{
			if (code.IsEmpty())
				throw new ArgumentNullException("code");

			return _boards.TryGetValue(code);
		}

		/// <summary>
		/// Получить биржу по коду.
		/// </summary>
		/// <param name="code">Код биржи <see cref="Exchange.Name"/>.</param>
		/// <returns>Биржа. Если биржа с заданным кодом не существует, то будет возвращено <see langword="null"/>.</returns>
		public Exchange GetExchange(string code)
		{
			if (code.IsEmpty())
				throw new ArgumentNullException("code");

			return _exchanges.TryGetValue(code);
		}
	}
}