namespace StockSharp.Algo
{
	using System;

	using StockSharp.Messages;

	/// <summary>
	/// Котировка с меткой времени. Используется для CSV файлов.
	/// </summary>
	public class TimeQuoteChange : QuoteChange
	{
		/// <summary>
		/// Создать <see cref="TimeQuoteChange"/>.
		/// </summary>
		public TimeQuoteChange()
		{
		}

		/// <summary>
		/// Создать <see cref="TimeQuoteChange"/>.
		/// </summary>
		/// <param name="quote">Котировка, из которой будут скопированы изменения.</param>
		/// <param name="message">Сообщение с котировками.</param>
		public TimeQuoteChange(QuoteChange quote, QuoteChangeMessage message)
		{
			if (quote == null)
				throw new ArgumentNullException("quote");

			SecurityId = message.SecurityId;
			ServerTime = message.ServerTime;
			LocalTime = message.LocalTime;
			Price = quote.Price;
			Volume = quote.Volume;
			Side = quote.Side;
		}

		/// <summary>
		/// Идентификатор инструмента.
		/// </summary>
		public SecurityId SecurityId { get; set; }

		/// <summary>
		/// Серверная метка времени.
		/// </summary>
		public DateTimeOffset ServerTime { get; set; }

		/// <summary>
		/// Локальная метка времени.
		/// </summary>
		public DateTime LocalTime { get; set; }
	}
}