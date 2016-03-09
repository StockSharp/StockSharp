#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Configuration.ConfigurationPublic
File: Extensions.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Configuration
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Data.Common;
	using System.IO;
	using System.Linq;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Data;
	using Ecng.Data.Sql;
	using Ecng.Interop;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.AlfaDirect;
	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Indicators;
	using StockSharp.Algo.Storages;
	using StockSharp.BarChart;
	using StockSharp.BitStamp;
	using StockSharp.Blackwood;
	using StockSharp.Btce;
	using StockSharp.CQG;
	using StockSharp.ETrade;
	using StockSharp.Fix;
	using StockSharp.InteractiveBrokers;
	using StockSharp.IQFeed;
	using StockSharp.ITCH;
	using StockSharp.LMAX;
	using StockSharp.Logging;
	using StockSharp.Micex;
	using StockSharp.Oanda;
	using StockSharp.OpenECry;
	using StockSharp.Plaza;
	using StockSharp.Quik;
	using StockSharp.Quik.Lua;
	using StockSharp.Rithmic;
	using StockSharp.Rss;
	using StockSharp.SmartCom;
	using StockSharp.Sterling;
	using StockSharp.Transaq;
	using StockSharp.Xaml;
	using StockSharp.Xaml.Charting;
	using StockSharp.Xaml.Charting.IndicatorPainters;

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
			var section = ConfigManager.InnerConfig.Sections.OfType<StockSharpSection>().FirstOrDefault();

			if (section == null)
				return;

			_customConnections = SafeAdd<ConnectionElement, ConnectorInfo>(section.CustomConnections, elem => new ConnectorInfo(elem.Type.To<Type>()));
			_customIndicators = SafeAdd<IndicatorElement, IndicatorType>(section.CustomIndicators, elem => new IndicatorType(elem.Type.To<Type>(), elem.Painter.To<Type>()));
			_customCandles = SafeAdd<CandleElement, Type>(section.CustomCandles, elem => elem.Type.To<Type>());
			_customDiagramElements = SafeAdd<DiagramElement, Type>(section.CustomDiagramElements, elem => elem.Type.To<Type>());
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
				throw new ArgumentNullException(nameof(connector));

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
				throw new ArgumentNullException(nameof(adapter));

			if (owner == null)
				throw new ArgumentNullException(nameof(owner));

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
				throw new ArgumentNullException(nameof(wnd));

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
				throw new ArgumentNullException(nameof(chart));

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
					.Where(t => !t.IsAbstract && 
						t.IsSubclassOf(typeof(Xaml.Diagram.DiagramElement)) && 
						t != typeof(Xaml.Diagram.CompositionDiagramElement) &&
						t != typeof(Xaml.Diagram.ExportDiagramElement))
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
				.Where(t => !t.IsAbstract && t.IsCandle())
				.Concat(_customCandles)
				.ToArray());
		}

		/// <summary>
		/// First time database initialization.
		/// </summary>
		/// <param name="entityRegistry">The storage of trade objects.</param>
		/// <param name="databaseRaw">Raw bytes of database file.</param>
		/// <param name="init">Initialization callback.</param>
		/// <returns>Path to the database file.</returns>
		public static string FirstTimeInit(this IEntityRegistry entityRegistry, byte[] databaseRaw, Action<Database> init = null)
		{
			if (entityRegistry == null)
				throw new ArgumentNullException(nameof(entityRegistry));

			if (databaseRaw == null)
				throw new ArgumentNullException(nameof(databaseRaw));

			var database = entityRegistry.Storage as Database;
			return database?.FirstTimeInit(databaseRaw, init);
		}

		/// <summary>
		/// First time database initialization.
		/// </summary>
		/// <param name="database">The database.</param>
		/// <param name="databaseRaw">Raw bytes of database file.</param>
		/// <param name="init">Initialization callback.</param>
		/// <returns>Path to the database file.</returns>
		public static string FirstTimeInit(this Database database, byte[] databaseRaw, Action<Database> init = null)
		{
			if (database == null)
				throw new ArgumentNullException(nameof(database));

			if (databaseRaw == null)
				throw new ArgumentNullException(nameof(databaseRaw));

			var conStr = new DbConnectionStringBuilder
			{
				ConnectionString = database.ConnectionString
			};

			var dbFile = (string)conStr.Cast<KeyValuePair<string, object>>().ToDictionary(StringComparer.InvariantCultureIgnoreCase).TryGetValue("Data Source");

			if (dbFile == null)
				return null;

			dbFile = dbFile.Replace("%Documents%", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

			conStr["Data Source"] = dbFile;
			database.ConnectionString = conStr.ToString();

			dbFile.CreateDirIfNotExists();

			if (!File.Exists(dbFile))
			{
				databaseRaw.Save(dbFile);
				UpdateDatabaseWalMode(database);

				init?.Invoke(database);
			}

			return dbFile;
		}

		private static void UpdateDatabaseWalMode(Database database)
		{
			var walQuery = Query.Execute("PRAGMA journal_mode=WAL;");
			var walCmd = database.GetCommand(walQuery, null, new FieldList(), new FieldList(), false);
			database.Execute(walCmd, new SerializationItemCollection(), false);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="storage"></param>
		/// <param name="name"></param>
		/// <param name="load"></param>
		public static void TryLoadSettings<T>(this SettingsStorage storage, string name, Action<T> load)
		{
			try
			{
				var settings = storage.GetValue<T>(name);

				if (settings == null)
					return;

				load(settings);
			}
			catch (Exception excp)
			{
				ConfigManager.GetService<LogManager>().Application.AddErrorLog(excp);
			}
		}
	}
}