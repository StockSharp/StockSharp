namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Serialization;

	/// <summary>
	/// Security mapping result message.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	public class SecurityMappingMessage : Message
	{
		/// <summary>
		/// Initialize <see cref="SecurityMappingMessage"/>.
		/// </summary>
		public SecurityMappingMessage()
			: base(MessageTypes.SecurityMapping)
		{
		}

		/// <summary>
		/// Security identifier mapping.
		/// </summary>
		[DataMember]
		public SecurityIdMapping Mapping { get; set; }

		/// <summary>
		/// Storage name.
		/// </summary>
		[DataMember]
		public string StorageName { get; set; }

		/// <summary>
		/// Create a copy of <see cref="SecurityMappingMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return new SecurityMappingMessage
			{
				Mapping = Mapping.Clone(),
				StorageName = StorageName,
			};
		}

		/// <inheritdoc />
		public override string ToString()
			=> base.ToString() + $"Storage={StorageName},Mapping={Mapping}";
	}
}