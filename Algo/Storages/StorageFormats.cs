#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Algo
File: StorageFormats.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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