namespace StockSharp.Community
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// The profile information.
	/// </summary>
	[DataContract]
	public class Profile
	{
		/// <summary>
		/// Identifier.
		/// </summary>
		[DataMember]
		public long Id { get; set; }

		/// <summary>
		/// Password (not filled in when obtaining from the server).
		/// </summary>
		[DataMember]
		public string Password { get; set; }

		/// <summary>
		/// Display name.
		/// </summary>
		[DataMember]
		public string DisplayName { get; set; }

		/// <summary>
		/// E-mail address.
		/// </summary>
		[DataMember]
		public string Email { get; set; }

		/// <summary>
		/// Phone.
		/// </summary>
		[DataMember]
		public string Phone { get; set; }

		/// <summary>
		/// Web site.
		/// </summary>
		[DataMember]
		public string Homepage { get; set; }

		/// <summary>
		/// Skype.
		/// </summary>
		[DataMember]
		public string Skype { get; set; }

		/// <summary>
		/// City.
		/// </summary>
		[DataMember]
		public string City { get; set; }

		/// <summary>
		/// Gender.
		/// </summary>
		[DataMember]
		public bool? Gender { get; set; }

		/// <summary>
		/// Is the mail-out enabled.
		/// </summary>
		[DataMember]
		public bool IsSubscription { get; set; }

		/// <summary>
		/// Language.
		/// </summary>
		[DataMember]
		public string Language { get; set; }

		/// <summary>
		/// Balance.
		/// </summary>
		[DataMember]
		public decimal Balance { get; set; }

		/// <summary>
		/// Balance.
		/// </summary>
		[DataMember]
		public long? Avatar { get; set; }

		/// <summary>
		/// Date of registration.
		/// </summary>
		[DataMember]
		public DateTime CreationDate { get; set; }
	}
}