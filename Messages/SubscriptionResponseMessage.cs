namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	/// <summary>
	/// Subscription response message.
	/// </summary>
	public class SubscriptionResponseMessage : BaseResultMessage<SubscriptionResponseMessage>, IErrorMessage
	{
		/// <summary>
		/// Not supported error.
		/// </summary>
		public static NotSupportedException NotSupported = new NotSupportedException();
		
		/// <inheritdoc />
		[DataMember]
		[XmlIgnore]
		public Exception Error { get; set; }

		/// <summary>
		/// Initialize <see cref="SubscriptionResponseMessage"/>.
		/// </summary>
		public SubscriptionResponseMessage()
			: base(MessageTypes.SubscriptionResponse)
		{
		}

		/// <inheritdoc />
		protected override void CopyTo(SubscriptionResponseMessage destination)
		{
			base.CopyTo(destination);

			destination.Error = Error;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			var str = base.ToString();

			if (Error != null)
				str += $",Error={Error.Message}";

			return str;
		}
	}
}