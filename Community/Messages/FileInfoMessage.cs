namespace StockSharp.Community.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// File info message.
	/// </summary>
	public class FileInfoMessage : Message, IOriginalTransactionIdMessage
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
		public long OriginalTransactionId { get; set; }

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
		public DateTime CreationDate { get; set; }

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		protected virtual void CopyTo(FileInfoMessage destination)
		{
			base.CopyTo(destination);

			destination.OriginalTransactionId = OriginalTransactionId;
			destination.BodyLength = BodyLength;
			destination.Body = Body;
			destination.FileName = FileName;
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

		/// <inheritdoc />
		public override string ToString()
		{
			var str = base.ToString() + $",OrigTrId={OriginalTransactionId}";

			if (!FileName.IsEmpty())
				str += $",FileName={FileName}";

			str += $",BodyLen={BodyLength}";

			if (Id != 0)
				str += $",Id={Id}";

			if (GroupId != 0)
				str += $",Id={GroupId}";

			return str;
		}
	}


}