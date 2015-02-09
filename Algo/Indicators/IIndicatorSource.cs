namespace StockSharp.Algo.Indicators
{
	using System;

	/// <summary>
	/// Интерфейс, описывающий источник данных для индикаторов.
	/// </summary>
	public interface IIndicatorSource
	{
		/// <summary>
		/// Событие появления новых данных.
		/// </summary>
		event Action<IIndicatorValue> NewValue;
	}
}