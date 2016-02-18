using System.Collections.Generic;
using StockSharp.Algo.Candles;

namespace StockSharp.Studio.Core.Commands {
	public class SubscribeCandleChartCommand : BaseStudioCommand {
		public CandleSeries Series {get;}

		public SubscribeCandleChartCommand(CandleSeries ser)
		{
			Series = ser;
		}
	}

	public class UnsubscribeCandleChartCommand : BaseStudioCommand {
		public CandleSeries Series {get;}

		public UnsubscribeCandleChartCommand(CandleSeries ser)
		{
			Series = ser;
		}
	}

	public class CandleDataCommand : BaseStudioCommand
	{
		public CandleSeries Series {get;}
		public IEnumerable<TimeFrameCandle> Candles {get;}

		public CandleDataCommand(CandleSeries series, IEnumerable<TimeFrameCandle> candles)
		{
			Series = series;
			Candles = candles;
		}
	}

	public class CandleChartResetCommand : BaseStudioCommand {
	}
}
