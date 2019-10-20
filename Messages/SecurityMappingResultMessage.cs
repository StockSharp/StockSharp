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
	public class SecurityMappingResultMessage : BaseResultMessage<SecurityMappingResultMessage>
	{
		/// <summary>
		/// Initialize <see cref="SecurityMappingResultMessage"/>.
		/// </summary>
		public SecurityMappingResultMessage()
			: base(MessageTypes.SecurityMappingResult)
		{
		}

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
		protected override void CopyTo(SecurityMappingResultMessage destination)
		{
			base.CopyTo(destination);
			destination.Mapping = Mapping.ToDictionary(StringComparer.InvariantCultureIgnoreCase);
		}

		/// <inheritdoc />
		public override string ToString()
			=> base.ToString() + $",Mapping={Mapping.Select(m => m.ToString()).Join(",")}";
	}
}