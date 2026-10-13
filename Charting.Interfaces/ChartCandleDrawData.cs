namespace StockSharp.Charting;

/// <summary>
/// Candle data prepared for chart drawing.
/// </summary>
public readonly struct ChartCandleDrawData
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ChartCandleDrawData"/>.
	/// </summary>
	/// <param name="dataType"><see cref="Messages.DataType"/>.</param>
	/// <param name="securityId"><see cref="Messages.SecurityId"/>.</param>
	/// <param name="openPrice">Opening price.</param>
	/// <param name="highPrice">Highest price.</param>
	/// <param name="lowPrice">Lowest price.</param>
	/// <param name="closePrice">Closing price.</param>
	/// <param name="totalVolume">Total candle volume.</param>
	/// <param name="priceLevels">Price levels.</param>
	/// <param name="state">Candle state.</param>
	public ChartCandleDrawData(DataType dataType, SecurityId securityId, decimal openPrice, decimal highPrice, decimal lowPrice, decimal closePrice, decimal totalVolume, CandlePriceLevel[] priceLevels, CandleStates state)
	{
		DataType = dataType;
		SecurityId = securityId;
		OpenPrice = openPrice;
		HighPrice = highPrice;
		LowPrice = lowPrice;
		ClosePrice = closePrice;
		TotalVolume = totalVolume;
		PriceLevels = priceLevels;
		State = state;
	}

	/// <summary>
	/// <see cref="Messages.DataType"/>.
	/// </summary>
	public DataType DataType { get; }

	/// <summary>
	/// <see cref="Messages.SecurityId"/>.
	/// </summary>
	public SecurityId SecurityId { get; }

	/// <summary>
	/// Opening price.
	/// </summary>
	public decimal OpenPrice { get; }

	/// <summary>
	/// Highest price.
	/// </summary>
	public decimal HighPrice { get; }

	/// <summary>
	/// Lowest price.
	/// </summary>
	public decimal LowPrice { get; }

	/// <summary>
	/// Closing price.
	/// </summary>
	public decimal ClosePrice { get; }

	/// <summary>
	/// Total candle volume.
	/// </summary>
	public decimal TotalVolume { get; }

	/// <summary>
	/// Price levels.
	/// </summary>
	public CandlePriceLevel[] PriceLevels { get; }

	/// <summary>
	/// Candle state.
	/// </summary>
	public CandleStates State { get; }
}
