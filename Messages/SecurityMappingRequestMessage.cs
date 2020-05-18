namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Security mapping request message.
	/// </summary>
	[Serializable]
	[DataContract]
	public class SecurityMappingRequestMessage : BaseRequestMessage
	{
		/// <summary>
		/// Initialize <see cref="SecurityMappingRequestMessage"/>.
		/// </summary>
		public SecurityMappingRequestMessage()
			: base(MessageTypes.SecurityMappingRequest)
		{
		}

		/// <inheritdoc />
		public override DataType DataType => DataType.SecurityMapping;

		/// <summary>
		/// Create a copy of <see cref="SecurityMappingRequestMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new SecurityMappingRequestMessage();
			CopyTo(clone);
			return clone;
		}
	}
}