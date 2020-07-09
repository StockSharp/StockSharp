namespace StockSharp.Algo.Storages.Remote.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;

	using StockSharp.Community;
	using StockSharp.Messages;

	/// <summary>
	/// Remove file message (upload or download).
	/// </summary>
	public class RemoteFileMessage : FileInfoMessage, ISecurityIdMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RemoteFileMessage"/>.
		/// </summary>
		public RemoteFileMessage()
			: base(ExtendedMessageTypes.RemoteFile)
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
		/// Date.
		/// </summary>
		[DataMember]
		public DateTimeOffset Date { get; set; }

		/// <summary>
		/// Storage format.
		/// </summary>
		[DataMember]
		public StorageFormats Format { get; set; }

		/// <summary>
		/// Create a copy of <see cref="RemoteFileMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new RemoteFileMessage
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