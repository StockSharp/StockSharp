namespace StockSharp.Algo.Export
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;

	using Ecng.Common;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Базовый класс экспорта.
	/// </summary>
	public abstract class BaseExporter
	{
		private readonly Func<int, bool> _isCancelled;

		/// <summary>
		/// Инициализировать <see cref="BaseExporter"/>.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="arg">Параметр данных.</param>
		/// <param name="isCancelled">Обработчик, возвращающий признак прерывания экспорта.</param>
		/// <param name="path">Путь к файлу.</param>
		protected BaseExporter(Security security, object arg, Func<int, bool> isCancelled, string path)
		{
			//if (security == null)
			//	throw new ArgumentNullException("security");

			if (isCancelled == null)
				throw new ArgumentNullException("isCancelled");

			if (path.IsEmpty())
				throw new ArgumentNullException("path");

			Security = security;
			Arg = arg;
			_isCancelled = isCancelled;
			Path = path;
		}

		/// <summary>
		/// Инструмент.
		/// </summary>
		protected Security Security { get; private set; }

		/// <summary>
		/// Параметр данных.
		/// </summary>
		public object Arg { get; private set; }

		/// <summary>
		/// Путь к файлу.
		/// </summary>
		protected string Path { get; private set; }

		/// <summary>
		/// Экспортировать значения.
		/// </summary>
		/// <param name="dataType">Тип маркет-данных.</param>
		/// <param name="values">Значение.</param>
		public void Export(Type dataType, IEnumerable values)
		{
			if (values == null)
				throw new ArgumentNullException("values");

			var secId = Security.ToSecurityId();

			CultureInfo.InvariantCulture.DoInCulture(() =>
			{
				if (dataType == typeof(Trade))
					Export(((IEnumerable<Trade>)values).Select(t => t.ToMessage()));
				else if (dataType == typeof(MarketDepth))
					Export(((IEnumerable<MarketDepth>)values).Select(d => d.ToMessage()));
				else if (dataType == typeof(QuoteChangeMessage))
					Export((IEnumerable<QuoteChangeMessage>)values);
				//else if (dataType == typeof(SecurityChange))
				//	Export(((IEnumerable<SecurityChange>)values).ToMessages(Security.ToSecurityId()));
				else if (dataType == typeof(Level1ChangeMessage))
					Export((IEnumerable<Level1ChangeMessage>)values);
				else if (dataType == typeof(OrderLogItem))
					Export(((IEnumerable<OrderLogItem>)values).Select(i => i.ToMessage()));
				else if (dataType == typeof(ExecutionMessage))
					Export((IEnumerable<ExecutionMessage>)values);
				else if (dataType.IsSubclassOf(typeof(Candle)))
					Export(((IEnumerable<Candle>)values).Select(c => c.ToMessage()));
				else if (dataType.IsSubclassOf(typeof(CandleMessage)))
					Export((IEnumerable<CandleMessage>)values);
				else if (dataType == typeof(News))
					Export(((IEnumerable<News>)values).Select(s => s.ToMessage()));
				else if (dataType == typeof(NewsMessage))
					Export((IEnumerable<NewsMessage>)values);
				else if (dataType == typeof(Security))
					Export(((IEnumerable<Security>)values).Select(s => s.ToMessage(secId)));
				else if (dataType == typeof(SecurityMessage))
					Export((IEnumerable<SecurityMessage>)values);
				else
					throw new ArgumentOutOfRangeException("dataType", dataType, LocalizedStrings.Str721);
			});
		}

		/// <summary>
		/// Можно ли продолжать экспорт.
		/// </summary>
		/// <param name="exported">Количество экспотированных элементов с предыдущего вызова метода.</param>
		/// <returns><see langword="true"/>, если экспорт можно продолжить, иначе, <see langword="false"/>.</returns>
		protected bool CanProcess(int exported = 1)
		{
			return !_isCancelled(exported);
		}

		/// <summary>
		/// Экспортировать <see cref="QuoteChangeMessage"/>.
		/// </summary>
		/// <param name="messages">Сообщения.</param>
		protected abstract void Export(IEnumerable<QuoteChangeMessage> messages);

		/// <summary>
		/// Экспортировать <see cref="Level1ChangeMessage"/>.
		/// </summary>
		/// <param name="messages">Сообщения.</param>
		protected abstract void Export(IEnumerable<Level1ChangeMessage> messages);

		/// <summary>
		/// Экспортировать <see cref="ExecutionMessage"/>.
		/// </summary>
		/// <param name="messages">Сообщения.</param>
		protected abstract void Export(IEnumerable<ExecutionMessage> messages);

		/// <summary>
		/// Экспортировать <see cref="CandleMessage"/>.
		/// </summary>
		/// <param name="messages">Сообщения.</param>
		protected abstract void Export(IEnumerable<CandleMessage> messages);

		/// <summary>
		/// Экспортировать <see cref="NewsMessage"/>.
		/// </summary>
		/// <param name="messages">Сообщения.</param>
		protected abstract void Export(IEnumerable<NewsMessage> messages);

		/// <summary>
		/// Экспортировать <see cref="SecurityMessage"/>.
		/// </summary>
		/// <param name="messages">Сообщения.</param>
		protected abstract void Export(IEnumerable<SecurityMessage> messages);
	}
}