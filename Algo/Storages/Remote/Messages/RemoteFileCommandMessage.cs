namespace StockSharp.Algo.Storages.Remote.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;

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
		public DataType DataType { get; set; }

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

		private byte[] _body = ArrayHelper.Empty<byte>();

		/// <summary>
		/// License body.
		/// </summary>
		[DataMember]
		public byte[] Body
		{
			get => _body;
			set => _body = value ?? throw new ArgumentNullException(nameof(value));
		}

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
			return base.ToString() + $",SecId={SecurityId},Type={DataType},Start={StartDate},End={EndDate},Fmt={Format},BodyLen={Body.Length}";
		}
	}
}