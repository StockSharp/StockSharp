namespace StockSharp.Community
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// License lookup message.
	/// </summary>
	[DataContract]
	[Serializable]
	public class LicenseLookupMessage : BaseSubscriptionMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LicenseLookupMessage"/>.
		/// </summary>
		public LicenseLookupMessage()
			: base(CommunityMessageTypes.LicenseLookup)
		{
		}

		/// <inheritdoc />
		public override DataType DataType => CommunityMessageTypes.LicenseInfoType;

		/// <summary>
		/// Platform.
		/// </summary>
		[DataMember]
		public string Platform { get; set; }

		/// <summary>
		/// Hardware id.
		/// </summary>
		[DataMember]
		public string HardwareId { get; set; }

		/// <summary>
		/// Create a copy of <see cref="LicenseLookupMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new LicenseLookupMessage();
			CopyTo(clone);
			return clone;
		}

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		protected void CopyTo(LicenseLookupMessage destination)
		{
			if (destination == null)
				throw new ArgumentNullException(nameof(destination));

			base.CopyTo(destination);

			destination.Platform = Platform;
			destination.HardwareId = HardwareId;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + $",Platform={Platform},HardwareId={HardwareId}";
		}
	}
}