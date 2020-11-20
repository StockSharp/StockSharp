namespace StockSharp.Community
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// License request (create, renew, delete) message.
	/// </summary>
	[DataContract]
	[Serializable]
	public class LicenseRequestMessage : BaseRequestMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LicenseRequestMessage"/>.
		/// </summary>
		public LicenseRequestMessage()
			: base(CommunityMessageTypes.LicenseRequest)
		{
		}

		/// <inheritdoc />
		public override DataType DataType => CommunityMessageTypes.LicenseInfoType;

		/// <summary>
		/// Broker identifier.
		/// </summary>
		[DataMember]
		public long BrokerId { get; set; }

		/// <summary>
		/// Account.
		/// </summary>
		[DataMember]
		public string Account { get; set; }

		/// <summary>
		/// License identifier.
		/// </summary>
		[DataMember]
		public long LicenseId { get; set; }

		/// <summary>
		/// Hardware id of the computer for which the license is issued.
		/// </summary>
		[DataMember]
		public string HardwareId { get; set; }

		/// <summary>
		/// Command.
		/// </summary>
		[DataMember]
		public CommandTypes? Command { get; set; }

		/// <summary>
		/// Features.
		/// </summary>
		[DataMember]
		public string Features { get; set; }

		/// <summary>
		/// Issued to.
		/// </summary>
		[DataMember]
		public long IssuedTo { get; set; }

		/// <summary>
		/// License expiry date.
		/// </summary>
		[DataMember]
		public DateTimeOffset? ExpirationDate { get; set; }

		/// <summary>
		/// Create a copy of <see cref="LicenseRequestMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new LicenseRequestMessage();
			CopyTo(clone);
			return clone;
		}

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		protected void CopyTo(LicenseRequestMessage destination)
		{
			if (destination == null)
				throw new ArgumentNullException(nameof(destination));

			base.CopyTo(destination);

			destination.Account = Account;
			destination.BrokerId = BrokerId;
			destination.LicenseId = LicenseId;
			destination.HardwareId = HardwareId;
			destination.Command = Command;
			destination.Features = Features;
			destination.ExpirationDate = ExpirationDate;
		}

		/// <inheritdoc />
		public override string ToString()
			=> base.ToString() + $",TrId={TransactionId},HID={HardwareId},Broker={BrokerId},Account={Account},LicId={LicenseId},Cmd={Command},Ftrs={Features}";
	}
}