namespace StockSharp.Hydra.Blackwood
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Net;
	using System.Security;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Algo.Candles;
	using StockSharp.Blackwood;
	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Core;
	using StockSharp.Messages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[Category(TaskCategories.American)]
	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2281ParamsKey, _sourceName)]
	[TaskDoc("http://stocksharp.com/doc/html/89a8b34c-63cf-4623-bbb7-90251d53e8e6.htm")]
	class BlackwoodTask : ConnectorHydraTask<BlackwoodTrader>
	{
		private const string _sourceName = "Fusion/Blackwood";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class BlackwoodSettings : ConnectorHydraTaskSettings
		{
			public BlackwoodSettings(HydraTaskSettings settings)
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
			[DisplayNameLoc(LocalizedStrings.Str3694Key)]
			[DescriptionLoc(LocalizedStrings.Str3695Key)]
			[PropertyOrder(2)]
			public EndPoint HistoricalDataAddress
			{
				get { return ExtensionInfo["HistoricalDataAddress"].To<EndPoint>(); }
				set { ExtensionInfo["HistoricalDataAddress"] = value.To<string>(); }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str3696Key)]
			[DescriptionLoc(LocalizedStrings.Str3697Key)]
			[PropertyOrder(3)]
			public EndPoint MarketDataAddress
			{
				get { return ExtensionInfo["MarketDataAddress"].To<EndPoint>(); }
				set { ExtensionInfo["MarketDataAddress"] = value.To<string>(); }
			}

			[Browsable(true)]
			public override bool IsDownloadNews
			{
				get { return base.IsDownloadNews; }
				set { base.IsDownloadNews = value; }
			}
		}

		public BlackwoodTask()
		{
			_supportedCandleSeries = BlackwoodMessageAdapter.TimeFrames.Select(tf => new CandleSeries
			{
				CandleType = typeof(TimeFrameCandle),
				Arg = tf
			}).ToArray();
		}

		private BlackwoodSettings _settings;

		public override Uri Icon
		{
			get { return "bw_logo.png".GetResourceUrl(GetType()); }
		}

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		private readonly Type[] _supportedMarketDataTypes = { /*typeof(MarketDepth),*/ typeof(Candle), typeof(Trade), typeof(Level1ChangeMessage), typeof(ExecutionMessage) };

		public override IEnumerable<Type> SupportedMarketDataTypes
		{
			get { return _supportedMarketDataTypes; }
		}

		private readonly IEnumerable<CandleSeries> _supportedCandleSeries;

		public override IEnumerable<CandleSeries> SupportedCandleSeries
		{
			get { return _supportedCandleSeries; }
		}

		protected override MarketDataConnector<BlackwoodTrader> CreateConnector(HydraTaskSettings settings)
		{
			_settings = new BlackwoodSettings(settings);

			if (settings.IsDefault)
			{
				_settings.Login = string.Empty;
				_settings.Password = new SecureString();
				_settings.IsDownloadNews = true;
				_settings.SupportedLevel1Fields = Enumerator.GetValues<Level1Fields>();

				_settings.HistoricalDataAddress = new IPEndPoint(BlackwoodAddresses.WetBush, BlackwoodAddresses.HistoricalDataPort);
				_settings.MarketDataAddress = new IPEndPoint(BlackwoodAddresses.WetBush, BlackwoodAddresses.MarketDataPort);
			}

			return new MarketDataConnector<BlackwoodTrader>(EntityRegistry.Securities, this, () => new BlackwoodTrader
			{
				HistoricalDataAddress = _settings.HistoricalDataAddress,
				MarketDataAddress = _settings.MarketDataAddress,
				Login = _settings.Login,
				Password = _settings.Password.To<string>(),
			});
		}
	}
}