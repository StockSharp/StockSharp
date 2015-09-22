namespace StockSharp.Community
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// News.
	/// </summary>
	[DataContract]
	public class CommunityNews
	{
		/// <summary>
		/// News <see cref="CommunityNews"/>.
		/// </summary>
		public CommunityNews()
		{
		}

		/// <summary>
		/// News ID.
		/// </summary>
		[DataMember]
		public long Id { get; set; }

		/// <summary>
		/// The news update frequency (in hours).
		/// </summary>
		[DataMember]
		public int Frequency { get; set; }

		/// <summary>
		/// News ends.
		/// </summary>
		[DataMember]
		public DateTime EndDate { get; set; }

		/// <summary>
		/// Headline in English.
		/// </summary>
		[DataMember]
		public string EnglishTitle { get; set; }

		/// <summary>
		/// Text in English.
		/// </summary>
		[DataMember]
		public string EnglishBody { get; set; }

		/// <summary>
		/// Headline in Russian.
		/// </summary>
		[DataMember]
		public string RussianTitle { get; set; }

		/// <summary>
		/// Text in Russian.
		/// </summary>
		[DataMember]
		public string RussianBody { get; set; }
	}
}