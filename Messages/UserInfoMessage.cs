namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel.DataAnnotations;
	using System.Linq;
	using System.Net;
	using System.Runtime.Serialization;
	using System.Security;
	using System.Xml.Serialization;

	using Ecng.Collections;

	using StockSharp.Localization;

	/// <summary>
	/// The message contains information about user.
	/// </summary>
	[DataContract]
	[Serializable]
	public class UserInfoMessage : Message, ITransactionIdMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="UserInfoMessage"/>.
		/// </summary>
		public UserInfoMessage()
			: base(MessageTypes.UserInfo)
		{
		}

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

		[field: NonSerialized]
		private SecureString _password;

		/// <summary>
		/// Portfolio currency.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.PasswordKey,
			Description = LocalizedStrings.PasswordKey + LocalizedStrings.Dot,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 1)]
		public SecureString Password
		{
			get => _password;
			set => _password = value;
		}

		/// <inheritdoc />
		public long TransactionId { get; set; }

		/// <summary>
		/// ID of the original message <see cref="UserLookupMessage.TransactionId"/> for which this message is a response.
		/// </summary>
		[DataMember]
		public long OriginalTransactionId { get; set; }

		/// <summary>
		/// Is blocked.
		/// </summary>
		[DataMember]
		public bool IsBlocked { get; set; }

		private IEnumerable<IPAddress> _ipRestrictions = Enumerable.Empty<IPAddress>();

		/// <summary>
		/// IP address restrictions.
		/// </summary>
		[XmlIgnore]
		public IEnumerable<IPAddress> IpRestrictions
		{
			get => _ipRestrictions;
			set => _ipRestrictions = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Permission set.
		/// </summary>
		public IDictionary<UserPermissions, IDictionary<Tuple<string, string, object, DateTime?>, bool>> Permissions { get; } = new Dictionary<UserPermissions, IDictionary<Tuple<string, string, object, DateTime?>, bool>>();

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + $",Name={Login}";
		}

		/// <summary>
		/// Create a copy of <see cref="UserInfoMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return CopyTo(new UserInfoMessage());
		}

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		/// <returns>The object, to which copied information.</returns>
		protected UserInfoMessage CopyTo(UserInfoMessage destination)
		{
			base.CopyTo(destination);

			destination.Login = Login;
			destination.Password = Password;
			destination.OriginalTransactionId = OriginalTransactionId;
			destination.IsBlocked = IsBlocked;
			destination.IpRestrictions = IpRestrictions.ToArray();
			destination.Permissions.AddRange(Permissions.ToDictionary());

			return destination;
		}
	}
}