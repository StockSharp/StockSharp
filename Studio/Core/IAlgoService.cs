namespace StockSharp.Studio.Core
{
	using System;
	using System.Collections.Generic;

	using StockSharp.Xaml.Charting;

	public interface IAlgoService
	{
		IEnumerable<IndicatorType> IndicatorTypes { get; }
		IEnumerable<Type> CandleTypes { get; }
		IEnumerable<Type> DiagramElementTypes { get; }
	}
}