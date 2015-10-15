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