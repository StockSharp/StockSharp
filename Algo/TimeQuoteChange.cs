namespace StockSharp.Algo
{
	using System;

	using StockSharp.Messages;

	/// <summary>
	/// The quote with the time mark. It used for CSV files.
	/// </summary>
	public class TimeQuoteChange : QuoteChange
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TimeQuoteChange"/>.
		/// </summary>
		public TimeQuoteChange()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TimeQuoteChange"/>.
		/// </summary>
		/// <param name="quote">The quote, from which changes will be copied.</param>
		/// <param name="message">The message with quotes.</param>
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
		/// Security ID.
		/// </summary>
		public SecurityId SecurityId { get; set; }

		/// <summary>
		/// The server time mark.
		/// </summary>
		public DateTimeOffset ServerTime { get; set; }

		/// <summary>
		/// The local time mark.
		/// </summary>
		public DateTime LocalTime { get; set; }
	}
}