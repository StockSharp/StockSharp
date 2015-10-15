namespace StockSharp.Algo.Storages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.ComponentModel;

	/// <summary>
	/// Format types.
	/// </summary>
	[Serializable]
	[DataContract]
	public enum StorageFormats
	{
		/// <summary>
		/// The binary format StockSharp.
		/// </summary>
		[EnumDisplayName("BIN")]
		[EnumMember]
		Binary,
		
		/// <summary>
		/// The text format CSV.
		/// </summary>
		[EnumDisplayName("CSV")]
		[EnumMember]
		Csv,

		//Database
	}
}