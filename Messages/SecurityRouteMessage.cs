namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Security route response message.
	/// </summary>
	[Serializable]
	[DataContract]
	public class SecurityRouteMessage : BaseRouteMessage<SecurityRouteMessage>, ISecurityIdMessage
	{
		/// <summary>
		/// Initialize <see cref="SecurityRouteMessage"/>.
		/// </summary>
		public SecurityRouteMessage()
			: base(MessageTypes.SecurityRoute)
		{
		}

		/// <inheritdoc />
		[DataMember]
		public SecurityId SecurityId { get; set; }

		/// <summary>
		/// Market data type.
		/// </summary>
		[DataMember]
		public DataType DataType { get; set; }

		/// <inheritdoc />
		public override void CopyTo(SecurityRouteMessage destination)
		{
			base.CopyTo(destination);

			destination.SecurityId = SecurityId;
			destination.DataType = DataType;
		}
	}
}