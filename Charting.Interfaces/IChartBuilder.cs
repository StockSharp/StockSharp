namespace StockSharp.Charting;

/// <summary>
/// The interface for build chart parts.
/// </summary>
public interface IChartBuilder
{
	/// <summary>
	/// Create <see cref="IChartArea"/> instance.
	/// </summary>
	/// <returns><see cref="IChartArea"/> instance.</returns>
	IChartArea CreateArea();

	/// <summary>
	/// Create <see cref="IChartAxis"/> instance.
	/// </summary>
	/// <returns><see cref="IChartAxis"/> instance.</returns>
	IChartAxis CreateAxis();

	/// <summary>
	/// Create <see cref="IChartCandleElement"/> instance.
	/// </summary>
	/// <returns><see cref="IChartCandleElement"/> instance.</returns>
	IChartCandleElement CreateCandleElement();

	/// <summary>
	/// Create <see cref="IChartIndicatorElement"/> instance.
	/// </summary>
	/// <returns><see cref="IChartIndicatorElement"/> instance.</returns>
	IChartIndicatorElement CreateIndicatorElement();

	/// <summary>
	/// Create <see cref="IChartActiveOrdersElement"/> instance.
	/// </summary>
	/// <returns><see cref="IChartActiveOrdersElement"/> instance.</returns>
	IChartActiveOrdersElement CreateActiveOrdersElement();

	/// <summary>
	/// Create <see cref="IChartAnnotationElement"/> instance.
	/// </summary>
	/// <returns><see cref="IChartAnnotationElement"/> instance.</returns>
	IChartAnnotationElement CreateAnnotation();

	/// <summary>
	/// Create <see cref="IChartBandElement"/> instance.
	/// </summary>
	/// <returns><see cref="IChartBandElement"/> instance.</returns>
	IChartBandElement CreateBandElement();

	/// <summary>
	/// Create <see cref="IChartLineElement"/> instance.
	/// </summary>
	/// <returns><see cref="IChartLineElement"/> instance.</returns>
	IChartLineElement CreateLineElement();

	/// <summary>
	/// Create <see cref="IChartLineElement"/> instance.
	/// </summary>
	/// <returns><see cref="IChartLineElement"/> instance.</returns>
	IChartLineElement CreateBubbleElement();

	/// <summary>
	/// Create <see cref="IChartOrderElement"/> instance.
	/// </summary>
	/// <returns><see cref="IChartOrderElement"/> instance.</returns>
	IChartOrderElement CreateOrderElement();

	/// <summary>
	/// Create <see cref="IChartTradeElement"/> instance.
	/// </summary>
	/// <returns><see cref="IChartTradeElement"/> instance.</returns>
	IChartTradeElement CreateTradeElement();
}