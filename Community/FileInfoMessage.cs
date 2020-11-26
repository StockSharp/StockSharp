namespace StockSharp.Community
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// File info message.
	/// </summary>
	[DataContract]
	[Serializable]
	public class FileInfoMessage : BaseSubscriptionIdMessage<FileInfoMessage>, ITransactionIdMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="UserInfoMessage"/>.
		/// </summary>
		public FileInfoMessage()
			: this(CommunityMessageTypes.FileInfo)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="UserInfoMessage"/>.
		/// </summary>
		/// <param name="type">Message type.</param>
		protected FileInfoMessage(MessageTypes type)
			: base(type)
		{
		}

		/// <inheritdoc />
		[DataMember]
		public long TransactionId { get; set; }

		/// <summary>
		/// File name.
		/// </summary>
		[DataMember]
		public string FileName { get; set; }

		/// <summary>
		/// File body length.
		/// </summary>
		[DataMember]
		public long BodyLength { get; set; }

		/// <summary>
		/// File body.
		/// </summary>
		[DataMember]
		public byte[] Body { get; set; }

		/// <summary>
		/// Identifier.
		/// </summary>
		[DataMember]
		public long Id { get; set; }

		/// <summary>
		/// Group ID.
		/// </summary>
		[DataMember]
		public long GroupId { get; set; }

		/// <summary>
		/// Is the file available for public.
		/// </summary>
		[DataMember]
		public bool IsPublic { get; set; }

		/// <summary>
		/// File url.
		/// </summary>
		[DataMember]
		public string Url { get; set; }

		/// <summary>
		/// File hash.
		/// </summary>
		[DataMember]
		public string Hash { get; set; }

		/// <summary>
		/// Date of creation.
		/// </summary>
		[DataMember]
		public DateTimeOffset CreationDate { get; set; }

		/// <inheritdoc />
		public override DataType DataType => DataType.Create(typeof(FileInfoMessage), null);

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		public override void CopyTo(FileInfoMessage destination)
		{
			base.CopyTo(destination);

			destination.TransactionId = TransactionId;
			destination.FileName = FileName;
			destination.BodyLength = BodyLength;
			destination.Body = Body;
			destination.Id = Id;
			destination.GroupId = GroupId;
			destination.IsPublic = IsPublic;
			destination.Url = Url;
			destination.Hash = Hash;
			destination.CreationDate = CreationDate;
		}

		/// <summary>
		/// Create a copy of <see cref="FileInfoMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new FileInfoMessage();
			CopyTo(clone);
			return clone;
		}

		/// <summary>
		/// Get body length.
		/// </summary>
		/// <returns>File body length.</returns>
		public long GetBodyLength() => Body?.Length ?? BodyLength;

		/// <inheritdoc />
		public override string ToString()
		{
			var str = base.ToString();

			if (TransactionId > 0)
				str += $",TrId={TransactionId}";

			str += $",BodyLen={GetBodyLength()}";

			if (!FileName.IsEmpty())
				str += $",FileName={FileName}";

			if (Id != 0)
				str += $",Id={Id}";

			if (GroupId != 0)
				str += $",Id={GroupId}";

			if (IsPublic)
				str += $",Public={IsPublic}";

			if (!Url.IsEmpty())
				str += $",Url={Url}";

			if (!Hash.IsEmpty())
				str += $",Hash={Hash}";

			if (CreationDate != default)
				str += $",Created={CreationDate}";

			return str;
		}
	}
}