namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Message for iterate action.
	/// </summary>
	[DataContract]
	[Serializable]
	public class ActionMessage : Message
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ActionMessage"/>.
		/// </summary>
		public ActionMessage()
			: base(MessageTypes.Action)
		{
		}

		/// <summary>
		/// Transaction ID.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TransactionKey)]
		[DescriptionLoc(LocalizedStrings.TransactionIdKey, true)]
		[MainCategory]
		public long TransactionId { get; set; }

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + $",TrId={TransactionId}";
		}

		/// <summary>
		/// Create a copy of <see cref="ActionMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return CopyTo(new ActionMessage());
		}

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		/// <returns>The object, to which copied information.</returns>
		protected ActionMessage CopyTo(ActionMessage destination)
		{
			destination.TransactionId = TransactionId;

			this.CopyExtensionInfo(destination);

			return destination;
		}
	}
}