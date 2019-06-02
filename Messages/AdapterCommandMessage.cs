namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;

	using Ecng.Collections;

	/// <summary>
	/// Adapter command message.
	/// </summary>
	[Serializable]
	[DataContract]
	public class AdapterCommandMessage : Message
	{
		/// <summary>
		/// Initialize <see cref="AdapterCommandMessage"/>.
		/// </summary>
		public AdapterCommandMessage()
			: base(MessageTypes.AdapterCommand)
		{
		}

		/// <summary>
		/// Request identifier.
		/// </summary>
		[DataMember]
		public long TransactionId { get; set; }

		/// <summary>
		/// Command.
		/// </summary>
		[DataMember]
		public string Command { get; set; }

		/// <summary>
		/// Parameters.
		/// </summary>
		[DataMember]
		public IDictionary<string, Tuple<string, string>> Parameters { get; private set; } = new Dictionary<string, Tuple<string, string>>();

		/// <summary>
		/// Create a copy of <see cref="AdapterCommandMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return new AdapterCommandMessage
			{
				TransactionId = TransactionId,
				Command = Command,
				Parameters = Parameters.ToDictionary(),
			};
		}

		/// <inheritdoc />
		public override string ToString() => base.ToString() + $",TrId={TransactionId}";
	}
}