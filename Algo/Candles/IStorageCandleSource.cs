namespace StockSharp.Algo.Candles
{
	using StockSharp.Algo.Storages;

	/// <summary>
	/// Интерфейс источника свечек для <see cref="ICandleManager"/>, который загружает данные из внешнего хранилища.
	/// </summary>
	public interface IStorageCandleSource
	{
		/// <summary>
		/// Хранилище данных.
		/// </summary>
		IStorageRegistry StorageRegistry { get; set; }
	}
}