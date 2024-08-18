namespace StockSharp.Algo.Storages;

using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

using StockSharp.Localization;

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
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BinaryKey)]
	[EnumMember]
	Binary,
	
	/// <summary>
	/// The text format CSV.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CSVKey)]
	[EnumMember]
	Csv,

	//Database
}