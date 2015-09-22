namespace StockSharp.Community
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// User.
	/// </summary>
	[DataContract]
	public class User
	{
		/// <summary>
		/// Identifier.
		/// </summary>
		[DataMember]
		public long Id { get; set; }

		/// <summary>
		/// Name.
		/// </summary>
		[DataMember]
		public string Name { get; set; }

		/// <summary>
		/// Description.
		/// </summary>
		[DataMember]
		public string Description { get; set; }

		/// <summary>
		/// Registration date.
		/// </summary>
		[DataMember]
		public DateTime CreationDate { get; set; }
	}
}