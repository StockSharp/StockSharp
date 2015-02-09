namespace StockSharp.Algo.Export
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Xaml.Database;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Algo.Export.Database;
	using StockSharp.Algo.Export.Database.DbProviders;
	using StockSharp.Messages;

	/// <summary>
	/// Экспорт в базу данных.
	/// </summary>
	public class DatabaseExporter : BaseExporter
	{
		private readonly DatabaseConnectionPair _connection;

		/// <summary>
		/// Создать <see cref="DatabaseExporter"/>.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="arg">Параметр данных.</param>
		/// <param name="isCancelled">Обработчик, возвращающий признак прерывания экспорта.</param>
		/// <param name="connection">Подключение к БД.</param>
		public DatabaseExporter(Security security, object arg, Func<int, bool> isCancelled, DatabaseConnectionPair connection)
			: base(security, arg, isCancelled, connection.ToString())
		{
			_connection = connection;
			CheckUnique = true;
		}

		private int _batchSize = 50;

		/// <summary>
		/// Размер пакета передаваемых данных. По-умолчанию равен 50 элементам.
		/// </summary>
		public int BatchSize
		{
			get { return _batchSize; }
			set
			{
				if (value < 1)
					throw new ArgumentOutOfRangeException();

				_batchSize = value;
			}
		}

		/// <summary>
		/// Проверять уникальность данных в базе данных. Влияет на производительность. По-умолчанию включено.
		/// </summary>
		public bool CheckUnique { get; set; }

		/// <summary>
		/// Экспортировать <see cref="ExecutionMessage"/>.
		/// </summary>
		/// <param name="messages">Сообщения.</param>
		protected override void Export(IEnumerable<ExecutionMessage> messages)
		{
			switch ((ExecutionTypes)Arg)
			{
				case ExecutionTypes.Tick:
					Do(messages, () => new TradeTable(Security));
					break;
				case ExecutionTypes.OrderLog:
					Do(messages, () => new OrderLogTable(Security));
					break;
				case ExecutionTypes.Order:
					Do(messages, () => new ExecutionTable(Security));
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Экспортировать <see cref="QuoteChangeMessage"/>.
		/// </summary>
		/// <param name="messages">Сообщения.</param>
		protected override void Export(IEnumerable<QuoteChangeMessage> messages)
		{
			Do(messages.SelectMany(d => d.Asks.Concat(d.Bids).OrderByDescending(q => q.Price).Select(q => new TimeQuoteChange(q, d))), () => new MarketDepthQuoteTable(Security));
		}

		/// <summary>
		/// Экспортировать <see cref="Level1ChangeMessage"/>.
		/// </summary>
		/// <param name="messages">Сообщения.</param>
		protected override void Export(IEnumerable<Level1ChangeMessage> messages)
		{
			Do(messages, () => new Level1Table(Security));
		}

		/// <summary>
		/// Экспортировать <see cref="CandleMessage"/>.
		/// </summary>
		/// <param name="messages">Сообщения.</param>
		protected override void Export(IEnumerable<CandleMessage> messages)
		{
			// TODO
			Do(messages, () => new CandlesTable(Security, typeof(TimeFrameCandle), Arg));
		}

		/// <summary>
		/// Экспортировать <see cref="NewsMessage"/>.
		/// </summary>
		/// <param name="messages">Сообщения.</param>
		protected override void Export(IEnumerable<NewsMessage> messages)
		{
			Do(messages, () => new NewsTable());
		}

		/// <summary>
		/// Экспортировать <see cref="SecurityMessage"/>.
		/// </summary>
		/// <param name="messages">Сообщения.</param>
		protected override void Export(IEnumerable<SecurityMessage> messages)
		{
			Do(messages, () => new SecuritiesTable(Security));
		}

		private void Do<TValue, TTable>(IEnumerable<TValue> values, Func<TTable> getTable)
			where TTable : Table<TValue>
		{
			if (getTable == null)
				throw new ArgumentNullException("getTable");

			using (var provider = BaseDbProvider.Create(_connection))
			{
				provider.CheckUnique = CheckUnique;

				var table = getTable();

				provider.CreateIfNotExists(table);

				foreach (var batch in values.Batch(BatchSize).Select(b => b.ToArray()))
				{
					if (!CanProcess(batch.Length))
						break;

					provider.InsertBatch(table, table.ConvertToParameters(batch));
				}
			}
		}
	}
}