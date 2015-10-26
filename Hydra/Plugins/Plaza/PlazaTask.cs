namespace StockSharp.Hydra.Plaza
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Net;
	using System.Security;

	using Ecng.Common;
	using Ecng.Localization;
	using Ecng.ComponentModel;

	using MoreLinq;

	using StockSharp.Hydra.Core;
	using StockSharp.Messages;
	using StockSharp.Plaza;
	using StockSharp.BusinessEntities;
	using StockSharp.Plaza.Xaml;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2281ParamsKey, _sourceName)]
	[TargetPlatform(Languages.Russian)]
	[Doc("http://stocksharp.com/doc/html/53930a42-ae5a-45fc-b9cf-8295584bf8fc.htm")]
	[TaskCategory(TaskCategories.Russia | TaskCategories.RealTime | TaskCategories.Stock |
		TaskCategories.Level1 | TaskCategories.MarketDepth | TaskCategories.Transactions |
		TaskCategories.Paid | TaskCategories.Ticks | TaskCategories.OrderLog)]
	class PlazaTask : ConnectorHydraTask<PlazaTrader>
	{
		private const string _sourceName = "Plaza2";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class PlazaSettings : ConnectorHydraTaskSettings
		{
			public PlazaSettings(HydraTaskSettings settings)
				: base(settings)
			{
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.LoginKey)]
			[DescriptionLoc(LocalizedStrings.LoginKey, true)]
			[PropertyOrder(0)]
			public string Login
			{
				get { return (string)ExtensionInfo["Login"]; }
				set { ExtensionInfo["Login"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.PasswordKey)]
			[DescriptionLoc(LocalizedStrings.PasswordKey, true)]
			[PropertyOrder(1)]
			public SecureString Password
			{
				get { return ExtensionInfo["Password"].To<SecureString>(); }
				set { ExtensionInfo["Password"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.AddressKey)]
			[DescriptionLoc(LocalizedStrings.AddressKey, true)]
			[PropertyOrder(2)]
			public EndPoint Address
			{
				get { return ExtensionInfo["Address"].To<EndPoint>(); }
				set { ExtensionInfo["Address"] = value.To<string>(); }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2595Key)]
			[DescriptionLoc(LocalizedStrings.Str2596Key)]
			[PropertyOrder(3)]
			public string AppName
			{
				get { return (string)ExtensionInfo["AppName"]; }
				set { ExtensionInfo["AppName"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.UseCGateKey)]
			[DescriptionLoc(LocalizedStrings.Str2798Key)]
			[PropertyOrder(4)]
			public bool IsCGate
			{
				get { return (bool)ExtensionInfo["IsCGate"]; }
				set { ExtensionInfo["IsCGate"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.CGateIdKey)]
			[DescriptionLoc(LocalizedStrings.Str2799Key)]
			[PropertyOrder(5)]
			public string CGateKey
			{
				get { return (string)ExtensionInfo["CGateKey"]; }
				set { ExtensionInfo["CGateKey"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2606Key)]
			[DescriptionLoc(LocalizedStrings.Str2607Key)]
			[PropertyOrder(6)]
			[Editor(typeof(PlazaTableListComboBoxEditor), typeof(PlazaTableListComboBoxEditor))]
			public IEnumerable<string> Tables
			{
				get { return (IEnumerable<string>)ExtensionInfo["Tables"]; }
				set { ExtensionInfo["Tables"] = value.ToArray(); }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2617Key)]
			[DescriptionLoc(LocalizedStrings.Str2800Key)]
			[PropertyOrder(7)]
			public bool OnlySystemTrades
			{
				get { return (bool)ExtensionInfo["OnlySystemTrades"]; }
				set { ExtensionInfo["OnlySystemTrades"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2801Key)]
			[DescriptionLoc(LocalizedStrings.Str2802Key)]
			[PropertyOrder(8)]
			public bool IsFastRepl
			{
				get { return (bool)ExtensionInfo["IsFastRepl"]; }
				set { ExtensionInfo["IsFastRepl"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.OverrideKey)]
			[DescriptionLoc(LocalizedStrings.OverrideDllKey)]
			[PropertyOrder(9)]
			public bool OverrideDll
			{
				get { return (bool)ExtensionInfo["OverrideDll"]; }
				set { ExtensionInfo["OverrideDll"] = value; }
			}
		}

		private int _changesCount;
		private PlazaSettings _settings;

		private readonly Type[] _supportedMarketDataTypes = { typeof(Trade), typeof(QuoteChangeMessage), typeof(OrderLogItem), typeof(Level1ChangeMessage), typeof(ExecutionMessage) };

		/// <summary>
		/// Поддерживаемые маркет-данные.
		/// </summary>
		public override IEnumerable<Type> SupportedMarketDataTypes
		{
			get { return _supportedMarketDataTypes; }
		}

		/// <summary>
		/// Загрузить порцию данных и сохранить их в хранилище.
		/// </summary>
		protected override TimeSpan OnProcess()
		{
			var interval = base.OnProcess();

			if (_changesCount++ > 100)
				SaveRevisions();

			return interval;
		}

		/// <summary>
		/// Остановить загрузку данных.
		/// </summary>
		protected override void OnStopped()
		{
			SaveRevisions();
			base.OnStopped();
		}

		private void SaveRevisions()
		{
			_changesCount = 0;
			Connector.Connector.StreamManager.RevisionManager.SaveRevisions();
		}

		protected override MarketDataConnector<PlazaTrader> CreateConnector(HydraTaskSettings settings)
		{
			_settings = new PlazaSettings(settings);

			if (settings.IsDefault)
			{
				using (var connector = new PlazaTrader())
				{
					_settings.AppName = "HYD";
					_settings.Address = connector.Address;
					_settings.Login = string.Empty;
					_settings.Password = new SecureString();
					_settings.IsCGate = false;
					_settings.CGateKey = PlazaMessageAdapter.DemoCGateKey;
					_settings.OnlySystemTrades = true;
					_settings.IsFastRepl = false;
					_settings.OverrideDll = true;

					var registry = connector.TableRegistry;
					_settings.Tables = new[]
					{
						registry.CommonFuture,
						registry.CommonOption,
						registry.SessionContentsFuture,
						registry.SessionContentsOption,
						registry.TradeFuture,
						registry.TradeOption,
						registry.Session,
						registry.Index,
						registry.Volatility,
						registry.Aggregation5Future,
						registry.Aggregation5Option,
						registry.AnonymousDeal
					}.Select(t => t.Id);
				}
			}

			return new MarketDataConnector<PlazaTrader>(EntityRegistry.Securities, this, CreatePlazaTrader);
		}

		private PlazaTrader CreatePlazaTrader()
		{
			var connector = new PlazaTrader
			{
				Address = _settings.Address,
				Login = _settings.Login,
				Password = _settings.Password.To<string>(),
				AppName = _settings.AppName,
				IsCGate = _settings.IsCGate,
				CGateKey = _settings.CGateKey,
				OnlySystemTrades = _settings.OnlySystemTrades,
				OverrideDll = _settings.OverrideDll
			};

			connector.TableRegistry.StreamRegistry.IsFastRepl = _settings.IsFastRepl;

			connector.TableRegistry.SyncTables(_settings.Tables);

			// добавляем все возможные колонки во все таблицы
			connector.Tables.ForEach(t => t.Metadata.AllColumns.ForEach(c => t.Columns.TryAdd(c)));

			// выключение авто-сохранения. ревизии теперь будут сохраняться вручную
			connector.StreamManager.RevisionManager.Interval = TimeSpan.Zero;

			// включаем отслеживание ревизий для таблиц
			connector.StreamManager.RevisionManager.Tables.Add(connector.TableRegistry.IndexLog); //не прокачивается таблица
			connector.StreamManager.RevisionManager.Tables.Add(connector.TableRegistry.TradeFuture);
			connector.StreamManager.RevisionManager.Tables.Add(connector.TableRegistry.TradeOption);
			connector.StreamManager.RevisionManager.Tables.Add(connector.TableRegistry.AnonymousOrdersLog);

			return connector;
		}

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}
	}
}