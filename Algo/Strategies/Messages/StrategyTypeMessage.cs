namespace StockSharp.Algo.Strategies.Messages
{
	using System;
	using System.Linq;
	using System.Runtime.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// The message contains information about strategy type.
	/// </summary>
	[DataContract]
	[Serializable]
	public class StrategyTypeMessage : Message, IOriginalTransactionIdMessage
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
		public string StrategyTypeId { get; set; }

		/// <summary>
		/// Strategy name.
		/// </summary>
		[DataMember]
		public string StrategyName { get; set; }

		/// <inheritdoc />
		[DataMember]
		public long OriginalTransactionId { get; set; }

		/// <summary>
		/// Assembly.
		/// </summary>
		[DataMember]
		public byte[] Assembly { get; set; }

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + $",Id={StrategyTypeId},Name={StrategyName},Asm={Assembly?.Length}";
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
			base.CopyTo(destination);

			destination.StrategyName = StrategyName;
			destination.StrategyTypeId = StrategyTypeId;
			destination.OriginalTransactionId = OriginalTransactionId;
			destination.Assembly = Assembly?.ToArray();

			return destination;
		}
	}
}