namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Security route response message.
	/// </summary>
	[Serializable]
	[DataContract]
	public class SecurityRouteMessage : BaseRouteMessage<SecurityRouteMessage>
	{
		/// <summary>
		/// Initialize <see cref="SecurityRouteMessage"/>.
		/// </summary>
		public SecurityRouteMessage()
			: base(MessageTypes.SecurityRoute)
		{
		}

		/// <summary>
		/// Security ID.
		/// </summary>
		[DataMember]
		public SecurityId SecurityId { get; set; }

		/// <summary>
		/// Market data type.
		/// </summary>
		[DataMember]
		public MarketDataTypes? DataType { get; set; }

		/// <inheritdoc />
		protected override void CopyTo(SecurityRouteMessage destination)
		{
			base.CopyTo(destination);

			destination.SecurityId = SecurityId;
			destination.DataType = DataType;
		}
	}
}