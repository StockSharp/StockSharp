namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	using Ecng.Collections;

	/// <summary>
	/// Adapter commands.
	/// </summary>
	public enum AdapterCommands
	{
		/// <summary>
		/// Connect.
		/// </summary>
		Connect,

		/// <summary>
		/// Disconnect.
		/// </summary>
		Disconnect,

		/// <summary>
		/// Enable.
		/// </summary>
		Enable,

		/// <summary>
		/// Disable.
		/// </summary>
		Disable,

		/// <summary>
		/// Update settings.
		/// </summary>
		Update,

		/// <summary>
		/// Remove.
		/// </summary>
		Remove,

		/// <summary>
		/// Request current state.
		/// </summary>
		RequestState,
	}

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
		/// Adapter identifier.
		/// </summary>
		[DataMember]
		public Guid AdapterId { get; set; }

		/// <summary>
		/// Command.
		/// </summary>
		[DataMember]
		public AdapterCommands Command { get; set; }

		/// <summary>
		/// Parameters.
		/// </summary>
		[DataMember]
		[XmlIgnore]
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
				AdapterId = AdapterId,
			};
		}

		/// <inheritdoc />
		public override string ToString() => base.ToString() + $",TrId={TransactionId},Cmd={Command}";
	}
}