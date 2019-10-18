namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	using Ecng.Serialization;

	/// <summary>
	/// A message containing subscription identifiers.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public abstract class BaseSubscriptionIdMessage : Message, ISubscriptionIdMessage
	{
		/// <inheritdoc />
		[DataMember]
		public long OriginalTransactionId { get; set; }

		/// <inheritdoc />
		[Ignore]
		[XmlIgnore]
		public long SubscriptionId { get; set; }

		/// <inheritdoc />
		[Ignore]
		[XmlIgnore]
		public IEnumerable<long> SubscriptionIds { get; set; }

		/// <summary>
		/// Initialize <see cref="BaseSubscriptionIdMessage"/>.
		/// </summary>
		/// <param name="type">Message type.</param>
		protected BaseSubscriptionIdMessage(MessageTypes type)
			: base(type)
		{
		}

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		protected virtual void CopyTo(BaseSubscriptionIdMessage destination)
		{
			base.CopyTo(destination);

			destination.OriginalTransactionId = OriginalTransactionId;
			destination.SubscriptionId = SubscriptionId;
			destination.SubscriptionIds = SubscriptionIds;//?.ToArray();
		}
	}
}