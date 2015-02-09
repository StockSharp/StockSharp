namespace StockSharp.Algo.Storages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.ComponentModel;

	/// <summary>
	/// Типы форматов.
	/// </summary>
	[Serializable]
	[DataContract]
	public enum StorageFormats
	{
		/// <summary>
		/// Бинарный формат StockSharp.
		/// </summary>
		[EnumDisplayName("BIN")]
		[EnumMember]
		Binary,
		
		/// <summary>
		/// Текстовый формат CSV.
		/// </summary>
		[EnumDisplayName("CSV")]
		[EnumMember]
		Csv,

		//Database
	}
}