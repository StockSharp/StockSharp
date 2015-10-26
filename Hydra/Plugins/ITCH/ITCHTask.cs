namespace StockSharp.Hydra.ITCH
{
	using System.ComponentModel;
	using System.Net;
	using System.Security;

	using Ecng.Common;
	using Ecng.Net;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Hydra.Core;
	using StockSharp.ITCH;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2281ParamsKey, _sourceName)]
	// TODO
	//[Doc("")]
	[TaskCategory(TaskCategories.America | TaskCategories.RealTime | TaskCategories.OrderLog |
		TaskCategories.Level1 | TaskCategories.MarketDepth | TaskCategories.Stock |
		TaskCategories.Transactions | TaskCategories.Paid | TaskCategories.Ticks)]
	class ItchTask : ConnectorHydraTask<ItchTrader>
	{
		private const string _sourceName = "ITCH";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class ItchSettings : ConnectorHydraTaskSettings
		{
			public ItchSettings(HydraTaskSettings settings)
				: base(settings)
			{
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.MainKey)]
			[DescriptionLoc(LocalizedStrings.MainUDPKey, true)]
			[PropertyOrder(0)]
			[ExpandableObject]
			public MulticastSourceAddress PrimaryMulticast
			{
				get { return (MulticastSourceAddress)ExtensionInfo["PrimaryMulticast"]; }
				set { ExtensionInfo["PrimaryMulticast"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.DuplicateKey)]
			[DescriptionLoc(LocalizedStrings.DuplicateUDPKey, true)]
			[PropertyOrder(1)]
			[ExpandableObject]
			public MulticastSourceAddress DuplicateMulticast
			{
				get { return (MulticastSourceAddress)ExtensionInfo["DuplicateMulticast"]; }
				set { ExtensionInfo["DuplicateMulticast"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.RecoveryKey)]
			[DescriptionLoc(LocalizedStrings.RecoveryServerKey, true)]
			[PropertyOrder(2)]
			public EndPoint RecoveryAddress
			{
				get { return ExtensionInfo["RecoveryAddress"].To<EndPoint>(); }
				set { ExtensionInfo["RecoveryAddress"] = value.To<string>(); }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.ReplayKey)]
			[DescriptionLoc(LocalizedStrings.ReplayServerKey, true)]
			[PropertyOrder(2)]
			public EndPoint ReplayAddress
			{
				get { return ExtensionInfo["ReplayAddress"].To<EndPoint>(); }
				set { ExtensionInfo["ReplayAddress"] = value.To<string>(); }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.LoginKey)]
			[DescriptionLoc(LocalizedStrings.LoginKey, true)]
			[PropertyOrder(3)]
			public string Login
			{
				get { return (string)ExtensionInfo["Login"]; }
				set { ExtensionInfo["Login"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.PasswordKey)]
			[DescriptionLoc(LocalizedStrings.PasswordKey, true)]
			[PropertyOrder(4)]
			public SecureString Password
			{
				get { return ExtensionInfo["Password"].To<SecureString>(); }
				set { ExtensionInfo["Password"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.SecuritiesKey)]
			[DescriptionLoc(LocalizedStrings.Str2137Key, true)]
			[PropertyOrder(5)]
			[Editor(typeof(FileBrowserEditor), typeof(FileBrowserEditor))]
			public string SecurityCsvFile
			{
				get { return (string)ExtensionInfo["SecurityCsvFile"]; }
				set { ExtensionInfo["SecurityCsvFile"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.XamlStr23Key)]
			[DescriptionLoc(LocalizedStrings.OnlyActiveSecuritiesKey)]
			[PropertyOrder(6)]
			public bool OnlyActiveSecurities
			{
				get { return (bool)ExtensionInfo["OnlyActiveSecurities"]; }
				set { ExtensionInfo["OnlyActiveSecurities"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.GroupIdKey)]
			[DescriptionLoc(LocalizedStrings.GroupIdKey, true)]
			[PropertyOrder(7)]
			public char GroupId
			{
				get { return (char)ExtensionInfo["GroupId"]; }
				set { ExtensionInfo["GroupId"] = value; }
			}

			public override HydraTaskSettings Clone()
			{
				var clone = (ItchSettings)base.Clone();
				clone.PrimaryMulticast = PrimaryMulticast.Clone();
				clone.DuplicateMulticast = DuplicateMulticast.Clone();
				return clone;
			}
		}

		private ItchSettings _settings;

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		protected override MarketDataConnector<ItchTrader> CreateConnector(HydraTaskSettings settings)
		{
			_settings = new ItchSettings(settings);

			if (settings.IsDefault)
			{
				_settings.PrimaryMulticast = new MulticastSourceAddress
				{
					GroupAddress = IPAddress.Any,
					Port = 1,
					SourceAddress = IPAddress.Any,
				};
				_settings.DuplicateMulticast = new MulticastSourceAddress
				{
					GroupAddress = IPAddress.Any,
					Port = 1,
					SourceAddress = IPAddress.Any,
				};
				_settings.Login = string.Empty;
				_settings.Password = new SecureString();
				_settings.ReplayAddress = _settings.RecoveryAddress = new IPEndPoint(IPAddress.Loopback, 3);
				_settings.SecurityCsvFile = string.Empty;
				_settings.OnlyActiveSecurities = true;
				_settings.GroupId = 'A';
			}

			return new MarketDataConnector<ItchTrader>(EntityRegistry.Securities, this, () => new ItchTrader
			{
				Login = _settings.Login,
				Password = _settings.Password.To<string>(),
				PrimaryMulticast = _settings.PrimaryMulticast,
				DuplicateMulticast = _settings.DuplicateMulticast,
				RecoveryAddress = _settings.RecoveryAddress,
				ReplayAddress = _settings.ReplayAddress,
				SecurityCsvFile = _settings.SecurityCsvFile,
				OnlyActiveSecurities = _settings.OnlyActiveSecurities,
				GroupId = _settings.GroupId
			});
		}
	}
}