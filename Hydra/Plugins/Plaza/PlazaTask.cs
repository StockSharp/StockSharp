#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Plaza.PlazaPublic
File: PlazaTask.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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

	using StockSharp.Algo;
	using StockSharp.Hydra.Core;
	using StockSharp.Messages;
	using StockSharp.Plaza;
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
	class PlazaTask : ConnectorHydraTask<PlazaMessageAdapter>
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
				get { return (string)ExtensionInfo[nameof(Login)]; }
				set { ExtensionInfo[nameof(Login)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.PasswordKey)]
			[DescriptionLoc(LocalizedStrings.PasswordKey, true)]
			[PropertyOrder(1)]
			public SecureString Password
			{
				get { return ExtensionInfo[nameof(Password)].To<SecureString>(); }
				set { ExtensionInfo[nameof(Password)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.AddressKey)]
			[DescriptionLoc(LocalizedStrings.AddressKey, true)]
			[PropertyOrder(2)]
			public EndPoint Address
			{
				get { return ExtensionInfo[nameof(Address)].To<EndPoint>(); }
				set { ExtensionInfo[nameof(Address)] = value.To<string>(); }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2595Key)]
			[DescriptionLoc(LocalizedStrings.Str2596Key)]
			[PropertyOrder(3)]
			public string AppName
			{
				get { return (string)ExtensionInfo[nameof(AppName)]; }
				set { ExtensionInfo[nameof(AppName)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.UseCGateKey)]
			[DescriptionLoc(LocalizedStrings.Str2798Key)]
			[PropertyOrder(4)]
			public bool IsCGate
			{
				get { return (bool)ExtensionInfo[nameof(IsCGate)]; }
				set { ExtensionInfo[nameof(IsCGate)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.CGateIdKey)]
			[DescriptionLoc(LocalizedStrings.Str2799Key)]
			[PropertyOrder(5)]
			public SecureString CGateKey
			{
				get { return ExtensionInfo[nameof(CGateKey)].To<SecureString>(); }
				set { ExtensionInfo[nameof(CGateKey)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2606Key)]
			[DescriptionLoc(LocalizedStrings.Str2607Key)]
			[PropertyOrder(6)]
			[Editor(typeof(PlazaTableListComboBoxEditor), typeof(PlazaTableListComboBoxEditor))]
			public IEnumerable<string> Tables
			{
				get { return (IEnumerable<string>)ExtensionInfo[nameof(Tables)]; }
				set { ExtensionInfo[nameof(Tables)] = value.ToArray(); }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2617Key)]
			[DescriptionLoc(LocalizedStrings.Str2800Key)]
			[PropertyOrder(7)]
			public bool OnlySystemTrades
			{
				get { return (bool)ExtensionInfo[nameof(OnlySystemTrades)]; }
				set { ExtensionInfo[nameof(OnlySystemTrades)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2801Key)]
			[DescriptionLoc(LocalizedStrings.Str2802Key)]
			[PropertyOrder(8)]
			public bool IsFastRepl
			{
				get { return (bool)ExtensionInfo[nameof(IsFastRepl)]; }
				set { ExtensionInfo[nameof(IsFastRepl)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.OverrideKey)]
			[DescriptionLoc(LocalizedStrings.OverrideDllKey)]
			[PropertyOrder(9)]
			public bool OverrideDll
			{
				get { return (bool)ExtensionInfo[nameof(OverrideDll)]; }
				set { ExtensionInfo[nameof(OverrideDll)] = value; }
			}
		}

		private int _changesCount;
		private PlazaSettings _settings;

		public override IEnumerable<DataType> SupportedDataTypes { get; } = new[]
		{
			DataType.Create(typeof(ExecutionMessage), ExecutionTypes.Tick),
			DataType.Create(typeof(ExecutionMessage), ExecutionTypes.Transaction),
			DataType.Create(typeof(ExecutionMessage), ExecutionTypes.OrderLog),
			DataType.Create(typeof(QuoteChangeMessage), null),
			DataType.Create(typeof(Level1ChangeMessage), null),
		};

		protected override TimeSpan OnProcess()
		{
			var interval = base.OnProcess();

			if (_changesCount++ > 100)
				SaveRevisions();

			return interval;
		}

		protected override void OnStopped()
		{
			SaveRevisions();
			base.OnStopped();
		}

		private void SaveRevisions()
		{
			_changesCount = 0;
			Adapter.StreamManager.RevisionManager.SaveRevisions();
		}

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new PlazaSettings(settings);

			if (!settings.IsDefault)
				return;

			using (var connector = new PlazaTrader())
			{
				_settings.AppName = "HYD";
				_settings.Address = connector.Address;
				_settings.Login = string.Empty;
				_settings.Password = new SecureString();
				_settings.CGateKey = new SecureString();
				_settings.IsCGate = false;
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

		protected override PlazaMessageAdapter GetAdapter(IdGenerator generator)
		{
			var adapter = new PlazaMessageAdapter(generator)
			{
				Address = _settings.Address,
				Login = _settings.Login,
				Password = _settings.Password,
				AppName = _settings.AppName,
				IsCGate = _settings.IsCGate,
				CGateKey = _settings.CGateKey,
				OnlySystemTrades = _settings.OnlySystemTrades,
				OverrideDll = _settings.OverrideDll
			};

			adapter.TableRegistry.StreamRegistry.IsFastRepl = _settings.IsFastRepl;

			adapter.TableRegistry.SyncTables(_settings.Tables);

			// добавляем все возможные колонки во все таблицы
			adapter.Tables.ForEach(t => t.Metadata.AllColumns.ForEach(c => t.Columns.TryAdd(c)));

			// выключение авто-сохранения. ревизии теперь будут сохраняться вручную
			adapter.StreamManager.RevisionManager.Interval = TimeSpan.Zero;

			// включаем отслеживание ревизий для таблиц
			adapter.StreamManager.RevisionManager.Tables.Add(adapter.TableRegistry.IndexLog); //не прокачивается таблица
			adapter.StreamManager.RevisionManager.Tables.Add(adapter.TableRegistry.TradeFuture);
			adapter.StreamManager.RevisionManager.Tables.Add(adapter.TableRegistry.TradeOption);
			adapter.StreamManager.RevisionManager.Tables.Add(adapter.TableRegistry.AnonymousOrdersLog);

			return adapter;
		}

		public override HydraTaskSettings Settings => _settings;
	}
}