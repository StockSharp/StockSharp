namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Base implementation of <see cref="ISubscriptionMessage"/> interface with non-online mode.
	/// </summary>
	[DataContract]
	[Serializable]
	public abstract class BaseRequestMessage : BaseSubscriptionMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BaseRequestMessage"/>.
		/// </summary>
		/// <param name="type">Message type.</param>
		public BaseRequestMessage(MessageTypes type)
			: base(type)
		{
		}

		/// <inheritdoc />
		[DataMember]
		public override DateTimeOffset? From => null;

		/// <inheritdoc />
		[DataMember]
		public override DateTimeOffset? To => DateTimeOffset.MaxValue /* prevent for online mode */;

		/// <inheritdoc />
		[DataMember]
		public override bool IsSubscribe => true;

		/// <inheritdoc />
		[DataMember]
		public override long OriginalTransactionId => 0;
	}
}