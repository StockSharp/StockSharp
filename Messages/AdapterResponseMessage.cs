namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	using Ecng.Collections;

	/// <summary>
	/// Adapter response message.
	/// </summary>
	[Serializable]
	[DataContract]
	public class AdapterResponseMessage : BaseResultMessage<AdapterResponseMessage>
	{
		/// <summary>
		/// Initialize <see cref="AdapterResponseMessage"/>.
		/// </summary>
		public AdapterResponseMessage()
			: base(MessageTypes.AdapterResponse)
		{
		}

		/// <summary>
		/// Adapter identifier.
		/// </summary>
		[DataMember]
		public Guid AdapterId { get; set; }

		/// <summary>
		/// Parameters.
		/// </summary>
		[DataMember]
		[XmlIgnore]
		public IDictionary<string, Tuple<string, string>> Parameters { get; private set; } = new Dictionary<string, Tuple<string, string>>();

		/// <inheritdoc />
		protected override void CopyTo(AdapterResponseMessage destination)
		{
			base.CopyTo(destination);

			destination.AdapterId = AdapterId;
			destination.Parameters = Parameters.ToDictionary();
		}
	}
}