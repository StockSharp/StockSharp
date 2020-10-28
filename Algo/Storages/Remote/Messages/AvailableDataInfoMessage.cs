namespace StockSharp.Algo.Storages.Remote.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// Available data info message.
	/// </summary>
	public class AvailableDataInfoMessage : BaseSubscriptionIdMessage<AvailableDataInfoMessage>, ISecurityIdMessage
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

		/// <inheritdoc />
		public override DataType DataType => DataType.Create(typeof(AvailableDataInfoMessage), null);

		/// <summary>
		/// Create a copy of <see cref="AvailableDataInfoMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new AvailableDataInfoMessage
			{
				SecurityId = SecurityId,
				FileDataType = FileDataType?.TypedClone(),
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