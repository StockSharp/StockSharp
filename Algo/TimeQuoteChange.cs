#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Algo
File: TimeQuoteChange.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo
{
	using System;

	using StockSharp.Messages;

	/// <summary>
	/// The quote with the time mark. It used for CSV files.
	/// </summary>
	public class TimeQuoteChange : IServerTimeMessage
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
		/// <param name="side">Direction (buy or sell).</param>
		/// <param name="quote">The quote, from which changes will be copied.</param>
		/// <param name="message">The message with quotes.</param>
		public TimeQuoteChange(Sides side, QuoteChange quote, QuoteChangeMessage message)
		{
			if (message is null)
				throw new ArgumentNullException(nameof(message));

			SecurityId = message.SecurityId;
			ServerTime = message.ServerTime;
			LocalTime = message.LocalTime;
			Quote = quote;
			Side = side;
		}

		/// <summary>
		/// Market depth quote representing bid or ask.
		/// </summary>
		public QuoteChange Quote { get; set; }

		/// <summary>
		/// Direction (buy or sell).
		/// </summary>
		public Sides Side { get; set; }

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
		public DateTimeOffset LocalTime { get; set; }

		MessageTypes IMessage.Type => throw new NotSupportedException();
		IMessageAdapter IMessage.Adapter { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
		MessageBackModes IMessage.BackMode { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
		object ICloneable.Clone() => throw new NotSupportedException();
	}
}