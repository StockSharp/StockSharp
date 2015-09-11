namespace StockSharp.Algo.Indicators
{
	using System.Collections.Generic;

	/// <summary>
	/// Интерфейс индикатора, который строится в виде комбинации нескольких индикаторов.
	/// </summary>
	public interface IComplexIndicator : IIndicator
	{
		/// <summary>
		/// Вложенные индикаторы.
		/// </summary>
		IEnumerable<IIndicator> InnerIndicators { get; }
	}
}