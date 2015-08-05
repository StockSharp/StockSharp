namespace StockSharp.Hydra.Rithmic
{
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

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2281ParamsKey, _sourceName)]
	[TaskDoc("http://stocksharp.com/doc/html/26ff0aad-623b-47e2-a8f8-a337506cd2ff.htm")]
	[TaskIcon("rithmic_logo.png")]
	[TaskCategory(TaskCategories.America | TaskCategories.RealTime | TaskCategories.Stock |
		TaskCategories.Free | TaskCategories.Ticks | TaskCategories.MarketDepth |
		TaskCategories.Level1 | TaskCategories.Candles | TaskCategories.Transactions)]
	class RithmicTask : ConnectorHydraTask<RithmicTrader>
	{
		private const string _sourceName = "Rithmic";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class RithmicSettings : ConnectorHydraTaskSettings
		{
			public RithmicSettings(HydraTaskSettings settings)
				: base(settings)
			{
			}

			/// <summary>
			/// Логин.
			/// </summary>
			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.LoginKey)]
			[DescriptionLoc(LocalizedStrings.LoginKey, true)]
			[PropertyOrder(0)]
			public string UserName
			{
				get { return (string)ExtensionInfo["UserName"]; }
				set { ExtensionInfo["UserName"] = value; }
			}

			/// <summary>
			/// Пароль.
			/// </summary>
			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.PasswordKey)]
			[DescriptionLoc(LocalizedStrings.PasswordKey, true)]
			[PropertyOrder(1)]
			public SecureString Password
			{
				get { return ExtensionInfo["Password"].To<SecureString>(); }
				set { ExtensionInfo["Password"] = value; }
			}

			/// <summary>
			/// Путь к файлу сертификата, необходимому для подключения к системе Rithmic.
			/// </summary>
			[CategoryLoc(_sourceName)]
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
			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str3416Key)]
			[DescriptionLoc(LocalizedStrings.Str3474Key)]
			[PropertyOrder(3)]
			public RithmicServers Server
			{
				get { return ExtensionInfo["Server"].To<RithmicServers>(); }
				set { ExtensionInfo["Server"] = value.To<string>(); }
			}

			/// <summary>
			/// Путь к лог файлу.
			/// </summary>
			[CategoryLoc(_sourceName)]
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
			_supportedCandleSeries = RithmicMessageAdapter.TimeFrames.Select(tf => new CandleSeries
			{
				CandleType = typeof(TimeFrameCandle),
				Arg = tf
			}).ToArray();
		}

		private RithmicSettings _settings;

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		private readonly IEnumerable<CandleSeries> _supportedCandleSeries;

		public override IEnumerable<CandleSeries> SupportedCandleSeries
		{
			get { return _supportedCandleSeries; }
		}

		protected override MarketDataConnector<RithmicTrader> CreateConnector(HydraTaskSettings settings)
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