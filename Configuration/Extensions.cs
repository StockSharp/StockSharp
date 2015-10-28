using StockSharp.Configuration.ConfigCandle;
using StockSharp.Configuration.ConfigConnection;
using StockSharp.Configuration.ConfigIndicator;

namespace StockSharp.Configuration
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using AlfaDirect;
	using Algo;
	using Algo.Candles;
	using Algo.Indicators;
	using BarChart;
	using BitStamp;
	using Blackwood;
	using Btce;
	using CQG;
	using ETrade;
	using Fix;
	using InteractiveBrokers;
	using IQFeed;
	using ITCH;
	using LMAX;
	using Logging;
	using Micex;
	using Oanda;
	using OpenECry;
	using Plaza;
	using Quik;
	using Quik.Lua;
	using Rithmic;
	using Rss;
	using SmartCom;
	using Sterling;
	using Transaq;
	using Xaml;
	using Xaml.Charting;
	using Xaml.Charting.IndicatorPainters;

	/// <summary>
	/// Extension class.
	/// </summary>
	public static class Extensions
	{
		private static readonly ConnectorInfo[] _customConnections = ArrayHelper.Empty<ConnectorInfo>();
		private static readonly IndicatorType[] _customIndicators = ArrayHelper.Empty<IndicatorType>();
		private static readonly Type[] _customCandles = ArrayHelper.Empty<Type>();
		private static readonly Type[] _customDiagramElements = ArrayHelper.Empty<Type>();

		static Extensions()
		{
            var section = Ecng.Configuration.ConfigManager.InnerConfig.Sections.OfType<StockSharpSection>().FirstOrDefault();

            if (section == null)
                return;

            _customConnections = SafeAdd<ConnectionElement, ConnectorInfo>(section.CustomConnections, elem => new ConnectorInfo(elem.Type.To<Type>()));
            _customIndicators = SafeAdd<IndicatorElement, IndicatorType>(section.CustomIndicators, elem => new IndicatorType(elem.Type.To<Type>(), elem.Painter.To<Type>()));
            _customCandles = SafeAdd<CandleElement, Type>(section.CustomCandles, elem => elem.Type.To<Type>());
            _customDiagramElements = SafeAdd<CandleElement, Type>(section.CustomDiagramElements, elem => elem.Type.To<Type>());
        }

		private static T2[] SafeAdd<T1, T2>(IEnumerable from, Func<T1, T2> func)
		{
			var list = new List<T2>();

			foreach (T1 item in from)
			{
				try
				{
					list.Add(func(item));
				}
				catch (Exception e)
				{
					e.LogError();
				}
			}

			return list.ToArray();
		}

		/// <summary>
		/// Configure connection using <see cref="ConnectorWindow"/>.
		/// </summary>
		/// <param name="connector">The connection.</param>
		/// <param name="owner">UI thread owner.</param>
		/// <returns><see langword="true"/> if the specified connection was configured, otherwise, <see langword="false"/>.</returns>
		public static bool Configure(this Connector connector, Window owner)
		{
			if (connector == null)
				throw new ArgumentNullException("connector");

			return connector.Adapter.Configure(owner);
		}

		/// <summary>
		/// Configure connection using <see cref="ConnectorWindow"/>.
		/// </summary>
		/// <param name="adapter">The connection.</param>
		/// <param name="owner">UI thread owner.</param>
		/// <returns><see langword="true"/> if the specified connection was configured, otherwise, <see langword="false"/>.</returns>
		public static bool Configure(this BasketMessageAdapter adapter, Window owner)
		{
			var autoConnect = false;
			return adapter.Configure(owner, ref autoConnect);
		}

		/// <summary>
		/// Configure connection using <see cref="ConnectorWindow"/>.
		/// </summary>
		/// <param name="adapter">The connection.</param>
		/// <param name="owner">UI thread owner.</param>
		/// <param name="autoConnect">Auto connect.</param>
		/// <returns><see langword="true"/> if the specified connection was configured, otherwise, <see langword="false"/>.</returns>
		public static bool Configure(this BasketMessageAdapter adapter, Window owner, ref bool autoConnect)
		{
			if (adapter == null)
				throw new ArgumentNullException("adapter");

			if (owner == null)
				throw new ArgumentNullException("owner");

			var wnd = new ConnectorWindow();

			wnd.ConnectorsInfo.AddRange(_customConnections);

			AddConnectorInfo(wnd, typeof(AlfaDirectMessageAdapter));
			AddConnectorInfo(wnd, typeof(BarChartMessageAdapter));
			AddConnectorInfo(wnd, typeof(BitStampMessageAdapter));
			AddConnectorInfo(wnd, typeof(BlackwoodMessageAdapter));
			AddConnectorInfo(wnd, typeof(BtceMessageAdapter));
			AddConnectorInfo(wnd, typeof(CQGMessageAdapter));
			AddConnectorInfo(wnd, typeof(ETradeMessageAdapter));
			AddConnectorInfo(wnd, typeof(FixMessageAdapter));
			AddConnectorInfo(wnd, typeof(InteractiveBrokersMessageAdapter));
			AddConnectorInfo(wnd, typeof(IQFeedMarketDataMessageAdapter));
			AddConnectorInfo(wnd, typeof(ItchMessageAdapter));
			AddConnectorInfo(wnd, typeof(LmaxMessageAdapter));
			AddConnectorInfo(wnd, typeof(MicexMessageAdapter));
			AddConnectorInfo(wnd, typeof(OandaMessageAdapter));
			AddConnectorInfo(wnd, typeof(OpenECryMessageAdapter));
			AddConnectorInfo(wnd, typeof(PlazaMessageAdapter));
			AddConnectorInfo(wnd, typeof(LuaFixTransactionMessageAdapter));
			AddConnectorInfo(wnd, typeof(LuaFixMarketDataMessageAdapter));
			AddConnectorInfo(wnd, typeof(QuikTrans2QuikAdapter));
			AddConnectorInfo(wnd, typeof(QuikDdeAdapter));
			AddConnectorInfo(wnd, typeof(RithmicMessageAdapter));
			AddConnectorInfo(wnd, typeof(RssMarketDataMessageAdapter));
			AddConnectorInfo(wnd, typeof(SmartComMessageAdapter));
			AddConnectorInfo(wnd, typeof(SterlingMessageAdapter));
			AddConnectorInfo(wnd, typeof(TransaqMessageAdapter));

			wnd.Adapter = (BasketMessageAdapter)adapter.Clone();
			wnd.AutoConnect = autoConnect;

			if (!wnd.ShowModal(owner))
				return false;

			adapter.Load(wnd.Adapter.Save());
			autoConnect = wnd.AutoConnect;

			return true;
		}

		private static void AddConnectorInfo(ConnectorWindow wnd, Type adapterType)
		{
			if (wnd == null)
				throw new ArgumentNullException("wnd");

			wnd.ConnectorsInfo.Add(new ConnectorInfo(adapterType));
		}

		private static IndicatorType[] _indicatorTypes;

		/// <summary>
		/// Get all indicator types.
		/// </summary>
		/// <returns>All indicator types.</returns>
		public static IEnumerable<IndicatorType> GetIndicatorTypes()
		{
			if (_indicatorTypes == null)
			{
				var ns = typeof(IIndicator).Namespace;

				var rendererTypes = typeof(Chart).Assembly
					.GetTypes()
					.Where(t => !t.IsAbstract && typeof(BaseChartIndicatorPainter).IsAssignableFrom(t))
					.ToDictionary(t => t.Name);

				_indicatorTypes = typeof(IIndicator).Assembly
					.GetTypes()
					.Where(t => t.Namespace == ns && !t.IsAbstract && typeof(IIndicator).IsAssignableFrom(t))
					.Select(t => new IndicatorType(t, rendererTypes.TryGetValue(t.Name + "Painter")))
					.Concat(_customIndicators)
					.ToArray();
			}

			return _indicatorTypes;
		}

		/// <summary>
		/// Fill <see cref="IChart.IndicatorTypes"/> using <see cref="GetIndicatorTypes"/>.
		/// </summary>
		/// <param name="chart">Chart.</param>
		public static void FillIndicators(this IChart chart)
		{
			if (chart == null)
				throw new ArgumentNullException("chart");

			chart.IndicatorTypes.Clear();
			chart.IndicatorTypes.AddRange(GetIndicatorTypes());
		}

		private static Type[] _diagramElements;

		/// <summary>
		/// Get all diagram elements.
		/// </summary>
		/// <returns>All diagram elements.</returns>
		public static IEnumerable<Xaml.Diagram.DiagramElement> GetDiagramElements()
		{
			if (_diagramElements == null)
			{
				_diagramElements = typeof(Xaml.Diagram.DiagramElement).Assembly
					.GetTypes()
					.Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(Xaml.Diagram.DiagramElement)))
					.Concat(_customDiagramElements)
					.ToArray();
			}

			return _diagramElements
				.Select(t => t.CreateInstance<Xaml.Diagram.DiagramElement>())
				.ToArray();
		}

		private static Type[] _candles;

		/// <summary>
		/// Get all candles.
		/// </summary>
		/// <returns>All candles.</returns>
		public static IEnumerable<Type> GetCandles()
		{
			return _candles ?? (_candles = typeof(Candle).Assembly
				.GetTypes()
				.Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(Candle)))
				.Concat(_customCandles)
				.ToArray());
		}
	}
}