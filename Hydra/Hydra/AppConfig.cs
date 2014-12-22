namespace StockSharp.Hydra
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;

	using StockSharp.Hydra.Configuration;
	using StockSharp.Logging;
	using StockSharp.Xaml.Charting;

	public class AppConfig
	{
		private readonly CachedSynchronizedList<IndicatorType> _indicators = new CachedSynchronizedList<IndicatorType>();
		private readonly CachedSynchronizedDictionary<Type, string> _candles = new CachedSynchronizedDictionary<Type, string>();

		private static AppConfig _instance;

		public static AppConfig Instance
		{
			get { return _instance ?? (_instance = new AppConfig()); }
		}

		private AppConfig()
		{
			var section = ConfigManager.GetSection<HydraSection>();

			SafeAdd<CandleElement>(section.Candles, elem => _candles.Add(elem.Type.To<Type>(), elem.Name));
			SafeAdd<IndicatorElement>(section.Indicators, elem => _indicators.Add(new IndicatorType(elem.Type.To<Type>(), elem.Painter.IsEmpty() ? null : elem.Painter.To<Type>())));

			_indicators.AddRange(_indicators.CopyAndClear().OrderBy(i => i.Name));
		}

		private static void SafeAdd<T1>(IEnumerable from, Action<T1> action)
		{
			foreach (T1 item in from)
			{
				try
				{
					action(item);
				}
				catch (Exception e)
				{
					e.LogError();
				}
			}
		}

		public IDictionary<Type, string> Candles
		{
			get { return _candles.CachedPairs.ToDictionary(); }
		}

		public IEnumerable<IndicatorType> Indicators
		{
			get { return _indicators.Cache; }
		}
	}
}