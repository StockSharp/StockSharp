namespace StockSharp.Messages
{
	using System.Runtime.Serialization;

	/// <summary>
	/// Subscription response message.
	/// </summary>
	public class SubscriptionResponseMessage : BaseResultMessage<SubscriptionResponseMessage>
	{
		/// <summary>
		/// Initialize <see cref="SubscriptionResponseMessage"/>.
		/// </summary>
		public SubscriptionResponseMessage()
			: base(MessageTypes.SubscriptionResponse)
		{
		}

		/// <summary>
		/// The message is not supported by adapter. To be set if the answer.
		/// </summary>
		[DataMember]
		public bool IsNotSupported { get; set; }

		/// <inheritdoc />
		protected override void CopyTo(SubscriptionResponseMessage destination)
		{
			base.CopyTo(destination);
			destination.IsNotSupported = IsNotSupported;
		}

		/// <inheritdoc />
		public override string ToString()
			=> base.ToString() + $",NotSup={IsNotSupported}";
	}
}