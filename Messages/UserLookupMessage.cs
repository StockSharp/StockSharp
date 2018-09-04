namespace StockSharp.Messages
{
	using System;
	using System.ComponentModel.DataAnnotations;
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Message users lookup for specified criteria.
	/// </summary>
	[DataContract]
	[Serializable]
	public class UserLookupMessage : Message
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="UserLookupMessage"/>.
		/// </summary>
		public UserLookupMessage()
			: base(MessageTypes.UserLookup)
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

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + $",Name={Login}";
		}

		/// <summary>
		/// Create a copy of <see cref="UserLookupMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return CopyTo(new UserLookupMessage());
		}

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		/// <returns>The object, to which copied information.</returns>
		protected UserLookupMessage CopyTo(UserLookupMessage destination)
		{
			destination.Login = Login;
			destination.TransactionId = TransactionId;

			this.CopyExtensionInfo(destination);

			return destination;
		}
	}
}