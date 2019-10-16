namespace StockSharp.Messages
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.Serialization;

	/// <summary>
	/// A message containing subscription identifiers.
	/// </summary>
	public abstract class BaseSubscriptionIdMessage : Message, ISubscriptionIdMessage
	{
		/// <inheritdoc />
		[DataMember]
		public long SubscriptionId { get; set; }

		/// <inheritdoc />
		[DataMember]
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

			destination.SubscriptionId = SubscriptionId;
			destination.SubscriptionIds = SubscriptionIds;//?.ToArray();
		}
	}
}