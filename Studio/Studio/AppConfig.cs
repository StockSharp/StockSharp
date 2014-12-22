namespace StockSharp.Studio
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;

	using StockSharp.Logging;
	using StockSharp.Studio.Configuration;
	using StockSharp.Xaml;
	using StockSharp.Xaml.Charting;

	public class AppConfig
	{
		private readonly CachedSynchronizedList<ConnectorInfo> _connections = new CachedSynchronizedList<ConnectorInfo>();
		private readonly CachedSynchronizedList<Type> _toolControls = new CachedSynchronizedList<Type>();
		private readonly CachedSynchronizedList<Type> _strategyControls = new CachedSynchronizedList<Type>();
		private readonly CachedSynchronizedList<IndicatorType> _indicators = new CachedSynchronizedList<IndicatorType>();
		private readonly CachedSynchronizedList<Type> _candles = new CachedSynchronizedList<Type>();
		private readonly CachedSynchronizedList<Type> _diagramElements = new CachedSynchronizedList<Type>();

		//static AppConfig()
		//{
		//    // а эксепшены кто ловить будет?
		//    // Instance = new AppConfig();
		//}

		private static AppConfig _instance;

		public static AppConfig Instance
		{
			get { return _instance ?? (_instance = new AppConfig()); }
		}

		private AppConfig()
		{
			var section = ConfigManager.GetSection<StudioSection>();

			FixServerAddresss = section.FixServerAddress;

			SafeAdd<ConnectionElement>(section.Connections, elem => _connections.Add(new ConnectorInfo(elem.SessionHolder.To<Type>(), elem.LogLevel)));
			SafeAdd<CandleElement>(section.Candles, elem => _candles.Add(elem.Type.To<Type>()));
			SafeAdd<IndicatorElement>(section.Indicators, elem => _indicators.Add(new IndicatorType(elem.Type.To<Type>(), elem.Painter.IsEmpty() ? null : elem.Painter.To<Type>())));

			_indicators.AddRange(_indicators.CopyAndClear().OrderBy(i => i.Name));

			SafeAdd<ControlElement>(section.ToolControls, elem => _toolControls.Add(elem.Type.To<Type>()));
			SafeAdd<ControlElement>(section.StrategyControls, elem => _strategyControls.Add(elem.Type.To<Type>()));

			SafeAdd<DiagramElement>(section.DiagramElements, elem => _diagramElements.Add(elem.Type.To<Type>()));
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

		public string FixServerAddresss { get; private set; }

		public IEnumerable<ConnectorInfo> Connections
		{
			get { return _connections.Cache; }
		}

		public IEnumerable<Type> Candles
		{
			get { return _candles.Cache; }
		}

		public IEnumerable<IndicatorType> Indicators
		{
			get { return _indicators.Cache; }
		}

		public IEnumerable<Type> ToolControls
		{
			get { return _toolControls.Cache; }
		}

		public IEnumerable<Type> StrategyControls
		{
			get { return _strategyControls.Cache; }
		}

		public IEnumerable<Type> DiagramElements
		{
			get { return _diagramElements.Cache; }
		}
	}
}
