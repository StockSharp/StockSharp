namespace StockSharp.Community
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// License feature message.
	/// </summary>
	[DataContract]
	[Serializable]
	public class LicenseFeatureMessage : BaseSubscriptionIdMessage<LicenseFeatureMessage>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LicenseFeatureMessage"/>.
		/// </summary>
		public LicenseFeatureMessage()
			: base(CommunityMessageTypes.LicenseFeature)
		{
		}

		/// <summary>
		/// Identifier.
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// Name.
		/// </summary>
		public string Name { get; set; }

		/// <inheritdoc />
		public override DataType DataType => CommunityMessageTypes.LicenseFeatureType;

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		public override void CopyTo(LicenseFeatureMessage destination)
		{
			if (destination == null)
				throw new ArgumentNullException(nameof(destination));

			base.CopyTo(destination);

			destination.Id = Id;
			destination.Name = Name;
		}

		/// <inheritdoc />
		public override string ToString() => base.ToString() + $",Id={Id},Name={Name}";
	}
}