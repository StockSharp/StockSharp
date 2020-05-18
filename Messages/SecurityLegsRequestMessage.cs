namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Security legs request message.
	/// </summary>
	[Serializable]
	[DataContract]
	public class SecurityLegsRequestMessage : BaseSubscriptionMessage
	{
		/// <summary>
		/// Initialize <see cref="SecurityLegsRequestMessage"/>.
		/// </summary>
		public SecurityLegsRequestMessage()
			: base(MessageTypes.SecurityLegsRequest)
		{
		}

		/// <inheritdoc />
		public override DataType DataType => DataType.Create(typeof(SecurityLegsInfoMessage), null);

		/// <summary>
		/// The filter for securities search.
		/// </summary>
		[DataMember]
		public string Like { get; set; }

		/// <summary>
		/// Create a copy of <see cref="SecurityLegsRequestMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new SecurityLegsRequestMessage
			{
				Like = Like,
			};

			CopyTo(clone);

			return clone;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + $",Like={Like},TrId={TransactionId}";
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