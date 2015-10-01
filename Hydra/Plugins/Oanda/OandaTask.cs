namespace StockSharp.Hydra.Oanda
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Security;

	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Hydra.Core;
	using StockSharp.Oanda;
	using StockSharp.Messages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2281ParamsKey, _sourceName)]
	[Doc("http://stocksharp.com/doc/html/6a99aea5-1142-4b2e-b183-554fcc890fad.htm")]
	[TaskCategory(TaskCategories.Forex | TaskCategories.RealTime |
		TaskCategories.Free | TaskCategories.History | TaskCategories.MarketDepth |
		TaskCategories.Level1 | TaskCategories.Candles | TaskCategories.Transactions)]
	class OandaTask : ConnectorHydraTask<OandaTrader>
	{
		private const string _sourceName = "OANDA";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class OandaSettings : ConnectorHydraTaskSettings
		{
			public OandaSettings(HydraTaskSettings settings)
				: base(settings)
			{
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str3416Key)]
			[DescriptionLoc(LocalizedStrings.Str3450Key)]
			[PropertyOrder(0)]
			public OandaServers Server
			{
				get { return ExtensionInfo["Server"].To<OandaServers>(); }
				set { ExtensionInfo["Server"] = value.To<string>(); }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str3451Key)]
			[DescriptionLoc(LocalizedStrings.Str3451Key, true)]
			[PropertyOrder(1)]
			public SecureString Token
			{
				get { return ExtensionInfo["Token"].To<SecureString>(); }
				set { ExtensionInfo["Token"] = value; }
			}
		}

		public OandaTask()
		{
			_supportedCandleSeries = OandaMessageAdapter.TimeFrames.Select(tf => new CandleSeries
			{
				CandleType = typeof(TimeFrameCandle),
				Arg = tf
			}).ToArray();
		}

		private OandaSettings _settings;

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		public override IEnumerable<Type> SupportedMarketDataTypes
		{
			get
			{
				return new[]
				{
					typeof(Candle),
					typeof(Level1ChangeMessage), 
					typeof(ExecutionMessage)
				}; 
			}
		}

		private readonly IEnumerable<CandleSeries> _supportedCandleSeries;

		public override IEnumerable<CandleSeries> SupportedCandleSeries
		{
			get { return _supportedCandleSeries; }
		}

		protected override MarketDataConnector<OandaTrader> CreateConnector(HydraTaskSettings settings)
		{
			_settings = new OandaSettings(settings);

			if (settings.IsDefault)
			{
				_settings.Server = OandaServers.Real;
				_settings.Token = new SecureString();
			}

			return new MarketDataConnector<OandaTrader>(EntityRegistry.Securities, this, () => new OandaTrader
			{
				Server = _settings.Server,
				Token = _settings.Token,
			});
		}
	}
}