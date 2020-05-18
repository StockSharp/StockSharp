namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	using Ecng.Collections;
	using Ecng.Common;

	/// <summary>
	/// Security mapping result message.
	/// </summary>
	[Serializable]
	[DataContract]
	public class SecurityMappingInfoMessage : BaseSubscriptionIdMessage<SecurityMappingInfoMessage>
	{
		/// <summary>
		/// Initialize <see cref="SecurityMappingInfoMessage"/>.
		/// </summary>
		public SecurityMappingInfoMessage()
			: base(MessageTypes.SecurityMappingInfo)
		{
		}

		/// <inheritdoc />
		public override DataType DataType => DataType.SecurityMapping;

		private IDictionary<string, IEnumerable<SecurityIdMapping>> _mapping = new Dictionary<string, IEnumerable<SecurityIdMapping>>(StringComparer.InvariantCultureIgnoreCase);

		/// <summary>
		/// Security identifier mapping.
		/// </summary>
		[DataMember]
		[XmlIgnore]
		public IDictionary<string, IEnumerable<SecurityIdMapping>> Mapping
		{
			get => _mapping;
			set => _mapping = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <inheritdoc />
		public override void CopyTo(SecurityMappingInfoMessage destination)
		{
			base.CopyTo(destination);
			destination.Mapping = Mapping.ToDictionary(StringComparer.InvariantCultureIgnoreCase);
		}

		/// <inheritdoc />
		public override string ToString()
			=> base.ToString() + $",Mapping={Mapping.Select(m => m.ToString()).JoinComma()}";
	}
}