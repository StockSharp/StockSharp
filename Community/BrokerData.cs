namespace StockSharp.Community
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Broker information.
	/// </summary>
	[DataContract]
	[Obsolete]
	public class BrokerData
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
		/// Criteria.
		/// </summary>
		[DataMember]
		public long[] Criteria { get; set; }

		/// <summary>
		/// Open an account link.
		/// </summary>
		[DataMember]
		public string OpenAccountLink { get; set; }

		/// <summary>
		/// Open a demo account link.
		/// </summary>
		[DataMember]
		public string OpenDemoAccountLink { get; set; }

		/// <summary>
		/// Picture id.
		/// </summary>
		[DataMember]
		public long? Picture { get; set; }
	}
}