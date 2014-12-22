namespace StockSharp.Hydra.Rithmic
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Security;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Algo.Candles;
	using StockSharp.Hydra.Core;
	using StockSharp.Messages;
	using StockSharp.Rithmic;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[Category(TaskCategories.American)]
	[TaskDisplayName(_sourceName)]
	class RithmicTask : ConnectorHydraTask<RithmicTrader>
	{
		private const string _sourceName = "Rithmic";

		[TaskSettingsDisplayName(_sourceName)]
		private sealed class RithmicSettings : ConnectorHydraTaskSettings
		{
			public RithmicSettings(HydraTaskSettings settings)
				: base(settings)
			{
			}

			/// <summary>
			/// Логин.
			/// </summary>
			[TaskCategory(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str1445Key)]
			[DescriptionLoc(LocalizedStrings.Str1445Key, true)]
			[PropertyOrder(0)]
			public string UserName
			{
				get { return (string)ExtensionInfo["UserName"]; }
				set { ExtensionInfo["UserName"] = value; }
			}

			/// <summary>
			/// Пароль.
			/// </summary>
			[TaskCategory(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str1447Key)]
			[DescriptionLoc(LocalizedStrings.Str1448Key)]
			[PropertyOrder(1)]
			public SecureString Password
			{
				get { return ExtensionInfo["Password"].To<SecureString>(); }
				set { ExtensionInfo["Password"] = value; }
			}

			/// <summary>
			/// Путь к файлу сертификата, необходимому для подключения к системе Rithmic.
			/// </summary>
			[TaskCategory(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str3465Key)]
			[DescriptionLoc(LocalizedStrings.Str3466Key)]
			[PropertyOrder(2)]
			[Editor(typeof(FileBrowserEditor), typeof(FileBrowserEditor))]
			public string CertFile
			{
				get { return (string)ExtensionInfo["CertFile"]; }
				set { ExtensionInfo["CertFile"] = value; }
			}

			/// <summary>
			/// Тип сервера.
			/// </summary>
			[TaskCategory(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str3416Key)]
			[DescriptionLoc(LocalizedStrings.Str3474Key)]
			[PropertyOrder(3)]
			public RithmicServers Server
			{
				get { return ExtensionInfo["RithmicServers"].To<RithmicServers>(); }
				set { ExtensionInfo["AdminConnectionPoint"] = value.To<string>(); }
			}

			/// <summary>
			/// Путь к лог файлу.
			/// </summary>
			[TaskCategory(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str3471Key)]
			[DescriptionLoc(LocalizedStrings.Str3472Key)]
			[PropertyOrder(4)]
			[Editor(typeof(FileBrowserEditor), typeof(FileBrowserEditor))]
			public string LogFileName
			{
				get { return (string)ExtensionInfo["LogFileName"]; }
				set { ExtensionInfo["LogFileName"] = value; }
			}
		}

		public RithmicTask()
		{
			_supportedCandleSeries = RithmicSessionHolder.TimeFrames.Select(tf => new CandleSeries
			{
				CandleType = typeof(TimeFrameCandle),
				Arg = tf
			}).ToArray();
		}

		private RithmicSettings _settings;

		public override Uri Icon
		{
			get { return "rithmic_logo.png".GetResourceUrl(GetType()); }
		}

		public override string Description
		{
			get { return LocalizedStrings.Str2281Params.Put(_sourceName); }
		}

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		private readonly IEnumerable<CandleSeries> _supportedCandleSeries;

		public override IEnumerable<CandleSeries> SupportedCandleSeries
		{
			get { return _supportedCandleSeries; }
		}

		protected override MarketDataConnector<RithmicTrader> CreateTrader(HydraTaskSettings settings)
		{
			_settings = new RithmicSettings(settings);

			if (settings.IsDefault)
			{
				_settings.UserName = string.Empty;
				_settings.Password = new SecureString();
				_settings.Server = RithmicServers.Paper;
				_settings.LogFileName = string.Empty;
				_settings.CertFile = string.Empty;
				_settings.SupportedLevel1Fields = Enumerator.GetValues<Level1Fields>();
			}

			return new MarketDataConnector<RithmicTrader>(EntityRegistry.Securities, this, () => new RithmicTrader
			{
				UserName = _settings.UserName,
				Password = _settings.Password.To<string>(),
				CertFile = _settings.CertFile,
				Server = _settings.Server,
				LogFileName = _settings.LogFileName
			});
		}
	}
}