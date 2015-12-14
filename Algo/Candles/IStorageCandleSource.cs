#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Candles.Algo
File: IStorageCandleSource.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Candles
{
	using StockSharp.Algo.Storages;

	/// <summary>
	/// The candles source interface for <see cref="ICandleManager"/> which loads data from external storage.
	/// </summary>
	public interface IStorageCandleSource
	{
		/// <summary>
		/// Market data storage.
		/// </summary>
		IStorageRegistry StorageRegistry { get; set; }
	}
}