#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: OrderStatusMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// A message requesting current registered orders and trades.
	/// </summary>
	[DataContract]
	[Serializable]
	public class OrderStatusMessage : OrderCancelMessage, ISubscriptionMessage
	{
		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str343Key)]
		[DescriptionLoc(LocalizedStrings.Str344Key)]
		[MainCategory]
		public DateTimeOffset? From { get; set; }

		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str345Key)]
		[DescriptionLoc(LocalizedStrings.Str346Key)]
		[MainCategory]
		public DateTimeOffset? To { get; set; }

		/// <inheritdoc />
		[DataMember]
		public long? Count { get; set; }

		/// <inheritdoc />
		[DataMember]
		public bool IsSubscribe { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="OrderStatusMessage"/>.
		/// </summary>
		public OrderStatusMessage()
			: base(MessageTypes.OrderStatus)
		{
		}

		DataType ISubscriptionMessage.DataType => DataType.Transactions;

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		protected void CopyTo(OrderStatusMessage destination)
		{
			base.CopyTo(destination);

			destination.From = From;
			destination.To = To;
			destination.Count = Count;
			destination.IsSubscribe = IsSubscribe;
		}

		/// <summary>
		/// Create a copy of <see cref="OrderStatusMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new OrderStatusMessage();
			CopyTo(clone);
			return clone;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			var str = base.ToString();

			str += $",IsSubscribe={IsSubscribe}";

			if (From != null)
				str += $",From={From.Value}";

			if (To != null)
				str += $",To={To.Value}";

			if (Count != null)
				str += $",Count={Count.Value}";

			return str;
		}
	}
}