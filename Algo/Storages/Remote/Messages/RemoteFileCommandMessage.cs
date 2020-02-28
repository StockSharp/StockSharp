namespace StockSharp.Algo.Storages.Remote.Messages
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// Remote file command.
	/// </summary>
	public class RemoteFileCommandMessage : CommandMessage, ISecurityIdMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RemoteFileCommandMessage"/>.
		/// </summary>
		public RemoteFileCommandMessage()
			: base(ExtendedMessageTypes.RemoteFileCommand)
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
		/// File body.
		/// </summary>
		[DataMember]
		public byte[] Body { get; set; }

		/// <summary>
		/// Create a copy of <see cref="RemoteFileCommandMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new RemoteFileCommandMessage
			{
				SecurityId = SecurityId,
				DataType = DataType,
				Arg = Arg,
				StartDate = StartDate,
				EndDate = EndDate,
				Format = Format,
				Body = Body,
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