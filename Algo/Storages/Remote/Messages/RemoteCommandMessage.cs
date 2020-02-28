namespace StockSharp.Algo.Storages.Remote.Messages
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// Remove file command.
	/// </summary>
	public class RemoteCommandMessage : CommandMessage, ISecurityIdMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RemoteCommandMessage"/>.
		/// </summary>
		public RemoteCommandMessage()
			: base(ExtendedMessageTypes.RemoteCommand)
		{
		}

		/// <inheritdoc />
		[DataMember]
		public SecurityId SecurityId { get; set; }

		/// <summary>
		/// Market data type.
		/// </summary>
		[DataMember]
		public MarketDataTypes DataType { get; set; }

		/// <summary>
		/// Additional argument for market data request.
		/// </summary>
		[DataMember]
		public object Arg { get; set; }

		/// <summary>
		/// Start date.
		/// </summary>
		[DataMember]
		public DateTimeOffset StartDate { get; set; }

		/// <summary>
		/// End date.
		/// </summary>
		[DataMember]
		public DateTimeOffset EndDate { get; set; }

		/// <summary>
		/// Storage format.
		/// </summary>
		[DataMember]
		public StorageFormats Format { get; set; }

		/// <summary>
		/// Create a copy of <see cref="RemoteCommandMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new RemoteCommandMessage
			{
				SecurityId = SecurityId,
				DataType = DataType,
				Arg = Arg,
				StartDate = StartDate,
				EndDate = EndDate,
				Format = Format,
			};

			CopyTo(clone);
			return clone;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + $",SecId={SecurityId},Type={DataType},Arg={Arg},Start={StartDate},End={EndDate},Fmt={Format}";
		}
	}
}