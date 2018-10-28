namespace StockSharp.Messages
{
	using System;
	using System.ComponentModel.DataAnnotations;
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// User request message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).
	/// </summary>
	[DataContract]
	[Serializable]
	public class UserRequestMessage : Message
	{
		/// <summary>
		/// Login.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.LoginKey,
			Description = LocalizedStrings.LoginKey + LocalizedStrings.Dot,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 0)]
		public string Login { get; set; }

		/// <summary>
		/// The message is subscription.
		/// </summary>
		[DataMember]
		public bool IsSubscribe { get; set; }

		/// <summary>
		/// Request identifier.
		/// </summary>
		[DataMember]
		public long TransactionId { get; set; }

		/// <summary>
		/// ID of the original message <see cref="TransactionId"/> for which this message is a response.
		/// </summary>
		[DataMember]
		public long OriginalTransactionId { get; set; }

		/// <summary>
		/// Subscribe or unsubscribe error info. To be set if the answer.
		/// </summary>
		[DataMember]
		public Exception Error { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="UserRequestMessage"/>.
		/// </summary>
		public UserRequestMessage()
			: base(MessageTypes.UserRequest)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="UserRequestMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return CopyTo(new UserRequestMessage());
		}

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		/// <returns>The object, to which copied information.</returns>
		protected UserRequestMessage CopyTo(UserRequestMessage destination)
		{
			destination.Login = Login;
			destination.IsSubscribe = IsSubscribe;
			destination.TransactionId = TransactionId;
			destination.OriginalTransactionId = OriginalTransactionId;
			destination.Error = Error;

			this.CopyExtensionInfo(destination);

			return destination;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + $",Login={Login},IsSubscribe={IsSubscribe},TrId={TransactionId},Origin={OriginalTransactionId}";
		}
	}
}