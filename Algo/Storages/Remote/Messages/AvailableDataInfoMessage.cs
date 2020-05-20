namespace StockSharp.Algo.Storages.Remote.Messages
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// Available data info message.
	/// </summary>
	public class AvailableDataInfoMessage : Message, ISecurityIdMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AvailableDataInfoMessage"/>.
		/// </summary>
		public AvailableDataInfoMessage()
			: base(ExtendedMessageTypes.AvailableDataInfo)
		{
		}

		/// <inheritdoc />
		[DataMember]
		public SecurityId SecurityId { get; set; }

		/// <summary>
		/// Market data type.
		/// </summary>
		[DataMember]
		public DataType FileDataType { get; set; }

		/// <summary>
		/// Start date.
		/// </summary>
		[DataMember]
		public DateTimeOffset Date { get; set; }

		/// <summary>
		/// Storage format.
		/// </summary>
		[DataMember]
		public StorageFormats Format { get; set; }

		/// <summary>
		/// Create a copy of <see cref="AvailableDataInfoMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new AvailableDataInfoMessage
			{
				SecurityId = SecurityId,
				FileDataType = FileDataType,
				Date = Date,
				Format = Format,
			};

			CopyTo(clone);
			return clone;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + $",SecId={SecurityId},DT={FileDataType},Date={Date},Fmt={Format}";
		}
	}
}