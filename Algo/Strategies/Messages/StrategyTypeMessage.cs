namespace StockSharp.Algo.Strategies.Messages
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// The message contains information about strategy type.
	/// </summary>
	[DataContract]
	[Serializable]
	public class StrategyTypeMessage : Message
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="StrategyTypeMessage"/>.
		/// </summary>
		public StrategyTypeMessage()
			: base(ExtendedMessageTypes.StrategyType)
		{
		}

		/// <summary>
		/// Strategy type ID.
		/// </summary>
		[DataMember]
		public Guid StrategyTypeId { get; set; }

		/// <summary>
		/// Strategy name.
		/// </summary>
		[DataMember]
		public string StrategyName { get; set; }

		/// <summary>
		/// ID of the original message <see cref="StrategyLookupMessage.TransactionId"/> for which this message is a response.
		/// </summary>
		[DataMember]
		public long OriginalTransactionId { get; set; }

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + $",Id={StrategyTypeId},Name={StrategyName}";
		}

		/// <summary>
		/// Create a copy of <see cref="StrategyTypeMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return CopyTo(new StrategyTypeMessage());
		}

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		/// <returns>The object, to which copied information.</returns>
		protected StrategyTypeMessage CopyTo(StrategyTypeMessage destination)
		{
			destination.StrategyName = StrategyName;
			destination.StrategyTypeId = StrategyTypeId;
			destination.OriginalTransactionId = OriginalTransactionId;

			this.CopyExtensionInfo(destination);

			return destination;
		}
	}
}