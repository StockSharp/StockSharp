namespace StockSharp.Algo.Strategies.Messages
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// Strategies search result message.
	/// </summary>
	[DataContract]
	[Serializable]
	public class StrategyLookupResultMessage : Message
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="StrategyLookupResultMessage"/>.
		/// </summary>
		public StrategyLookupResultMessage()
			: base(ExtendedMessageTypes.StrategyLookupResult)
		{
		}

		/// <summary>
		/// ID of the original message <see cref="StrategyLookupMessage.TransactionId"/> for which this message is a response.
		/// </summary>
		[DataMember]
		public long OriginalTransactionId { get; set; }

		/// <summary>
		/// Lookup error info.
		/// </summary>
		[DataMember]
		public Exception Error { get; set; }

		/// <summary>
		/// Create a copy of <see cref="StrategyLookupResultMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return CopyTo(new StrategyLookupResultMessage());
		}

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		/// <returns>The object, to which copied information.</returns>
		protected StrategyLookupResultMessage CopyTo(StrategyLookupResultMessage destination)
		{
			destination.OriginalTransactionId = OriginalTransactionId;
			destination.Error = Error;

			this.CopyExtensionInfo(destination);

			return destination;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			var str = base.ToString() + $",Orig={OriginalTransactionId}";

			if (Error != null)
				str += $",Error={Error.Message}";

			return str;
		}
	}
}