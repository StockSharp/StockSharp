namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;

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
		public override DataType DataType => DataType.SecurityRoute;

		/// <inheritdoc />
		[DataMember]
		public SecurityId SecurityId { get; set; }

		/// <summary>
		/// Market data type.
		/// </summary>
		[DataMember]
		public DataType SecurityDataType { get; set; }

		/// <inheritdoc />
		public override void CopyTo(SecurityRouteMessage destination)
		{
			base.CopyTo(destination);

			destination.SecurityId = SecurityId;
			destination.SecurityDataType = SecurityDataType?.TypedClone();
		}
	}
}