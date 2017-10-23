namespace StockSharp.Community
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// The file data.
	/// </summary>
	[DataContract]
	public class FileData
	{
		/// <summary>
		/// Identifier.
		/// </summary>
		[DataMember]
		public long Id { get; set; }

		/// <summary>
		/// File name.
		/// </summary>
		[DataMember]
		public string FileName { get; set; }

		/// <summary>
		/// File body.
		/// </summary>
		[DataMember]
		public byte[] Body { get; set; }

		/// <summary>
		/// File body length.
		/// </summary>
		[DataMember]
		public int BodyLength { get; set; }

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
		/// Date of creation.
		/// </summary>
		[DataMember]
		public DateTime CreationDate { get; set; }
	}
}