#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Testing.Algo
File: HistorySourceMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		public void CopyTo(HistorySourceMessage destination)
		{
			base.CopyTo(destination);

			destination.GetMessages = GetMessages;
		}

		/// <summary>
		/// Create a copy of <see cref="HistorySourceMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new HistorySourceMessage();
			CopyTo(clone);
			return clone;
		}
	}
}