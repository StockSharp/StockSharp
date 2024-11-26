using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StockSharp.Algo.Candles;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Storage.RemoteSource
{
    internal static class DataTypeExtensions
    {
		public static CandleSeries ToCandleSeries(this DataType dataType, Security security)
		{
			if (dataType is null)
				throw new ArgumentNullException(nameof(dataType));

			return new CandleSeries
			{
				CandleType = dataType.MessageType.ToCandleType(),
				Arg = dataType.Arg,
				Security = security,
			};
		}
    }
}
