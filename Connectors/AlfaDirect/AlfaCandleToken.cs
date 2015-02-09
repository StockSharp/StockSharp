namespace StockSharp.AlfaDirect
{
	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;

	sealed class AlfaCandleSeries : CandleSeries
	{
		public AlfaCandleSeries(Security security, AlfaTimeFrames timeFrame)
			: base(typeof(TimeFrameCandle), security, timeFrame)
		{
		}
	}
}