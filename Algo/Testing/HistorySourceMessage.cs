namespace StockSharp.Algo.Testing
{
	using System;
	using System.Collections.Generic;

	using StockSharp.Messages;

	/// <summary>
	/// Market-data message with historical source.
	/// </summary>
	public class HistorySourceMessage : MarketDataMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="HistorySourceMessage"/>.
		/// </summary>
		public HistorySourceMessage()
			: base(ExtendedMessageTypes.HistorySource)
		{
		}

		/// <summary>
		/// Callback to retrieve historical data for the specified date.
		/// </summary>
		public Func<DateTimeOffset, IEnumerable<Message>> GetMessages { get; set; }
	}
}