#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Candles.Compression.Algo
File: BaseCandleBuilderSource.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Candles.Compression
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Collections;

	using StockSharp.Messages;

	/// <summary>
	/// The base data source for <see cref="ICandleBuilder"/>.
	/// </summary>
	public abstract class BaseCandleBuilderSource : BaseCandleSource<IEnumerable<ICandleBuilderSourceValue>>, ICandleBuilderSource
	{
		private readonly Dictionary<CandleSeries, Tuple<DateTimeOffset, WorkingTimePeriod>> _currentPeriods = new Dictionary<CandleSeries, Tuple<DateTimeOffset, WorkingTimePeriod>>();

		/// <summary>
		/// Initialize <see cref="BaseCandleBuilderSource"/>.
		/// </summary>
		protected BaseCandleBuilderSource()
		{
		}

		/// <summary>
		/// To call the event <see cref="BaseCandleSource{TValue}.Processing"/>.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="values">New data.</param>
		protected override void RaiseProcessing(CandleSeries series, IEnumerable<ICandleBuilderSourceValue> values)
		{
			var tuple = _currentPeriods.TryGetValue(series);

			var filteredValues = values.Where(v =>
			{
				var time = v.Time;

				if (!(time >= series.From && time < series.To))
					return false;

				if (tuple == null || tuple.Item1.Date.Date != time.Date.Date)
					return CheckTime(series, time, out tuple);

				var exchangeTime = time.ToLocalTime(series.Security.Board.TimeZone);
				var tod = exchangeTime.TimeOfDay;
				var period = tuple.Item2;

				var res = period == null || period.Times.IsEmpty() || period.Times.Any(r => r.Contains(tod));

				if (res)
					return true;

				return CheckTime(series, time, out tuple);
			});

			base.RaiseProcessing(series, filteredValues);
		}

		private bool CheckTime(CandleSeries series, DateTimeOffset time, out Tuple<DateTimeOffset, WorkingTimePeriod> tuple)
		{
			var res = series.Security.Board.IsTradeTime(time, out var period);
			_currentPeriods[series] = tuple = Tuple.Create(time, period);

			return res;
		}
	}
}