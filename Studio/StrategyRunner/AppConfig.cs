namespace StockSharp.Studio.StrategyRunner
{
	using System;
	using System.Collections;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;

	using StockSharp.Logging;
	using StockSharp.Studio.StrategyRunner.Configuration;
	using StockSharp.Xaml;

	public class AppConfig
	{
		private readonly CachedSynchronizedList<ConnectorInfo> _connections = new CachedSynchronizedList<ConnectorInfo>();
		private readonly CachedSynchronizedList<Type> _diagramElements = new CachedSynchronizedList<Type>();

		private static AppConfig _instance;

		public static AppConfig Instance
		{
			get { return _instance ?? (_instance = new AppConfig()); }
		}

		private AppConfig()
		{
			var section = ConfigManager.GetSection<StockSharpSection>();

			SafeAdd<ConnectionElement>(section.Connections, elem => _connections.Add(new ConnectorInfo(elem.SessionHolder.To<Type>(), elem.LogLevel)));
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

		public IEnumerable<ConnectorInfo> Connections
		{
			get { return _connections.Cache; }
		}

		public IEnumerable<Type> DiagramElements
		{
			get { return _diagramElements.Cache; }
		}
	}
}