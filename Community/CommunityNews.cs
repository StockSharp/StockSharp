#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Community.Community
File: CommunityNews.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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