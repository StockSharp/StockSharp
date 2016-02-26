#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.ITCH.ITCHPublic
File: ITCHTask.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.ITCH
{
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Net;
	using System.Security;

	using Ecng.Common;
	using Ecng.Net;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.Hydra.Core;
	using StockSharp.ITCH;
	using StockSharp.Localization;
	using StockSharp.Messages;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2281ParamsKey, _sourceName)]
	// TODO
	//[Doc("")]
	[TaskCategory(TaskCategories.America | TaskCategories.RealTime | TaskCategories.OrderLog |
		TaskCategories.Level1 | TaskCategories.MarketDepth | TaskCategories.Stock |
		TaskCategories.Transactions | TaskCategories.Paid | TaskCategories.Ticks)]
	class ItchTask : ConnectorHydraTask<ItchMessageAdapter>
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
				get { return (MulticastSourceAddress)ExtensionInfo[nameof(PrimaryMulticast)]; }
				set { ExtensionInfo[nameof(PrimaryMulticast)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.DuplicateKey)]
			[DescriptionLoc(LocalizedStrings.DuplicateUDPKey, true)]
			[PropertyOrder(1)]
			[ExpandableObject]
			public MulticastSourceAddress DuplicateMulticast
			{
				get { return (MulticastSourceAddress)ExtensionInfo[nameof(DuplicateMulticast)]; }
				set { ExtensionInfo[nameof(DuplicateMulticast)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.RecoveryKey)]
			[DescriptionLoc(LocalizedStrings.RecoveryServerKey, true)]
			[PropertyOrder(2)]
			public EndPoint RecoveryAddress
			{
				get { return ExtensionInfo[nameof(RecoveryAddress)].To<EndPoint>(); }
				set { ExtensionInfo[nameof(RecoveryAddress)] = value.To<string>(); }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.ReplayKey)]
			[DescriptionLoc(LocalizedStrings.ReplayServerKey, true)]
			[PropertyOrder(2)]
			public EndPoint ReplayAddress
			{
				get { return ExtensionInfo[nameof(ReplayAddress)].To<EndPoint>(); }
				set { ExtensionInfo[nameof(ReplayAddress)] = value.To<string>(); }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.LoginKey)]
			[DescriptionLoc(LocalizedStrings.LoginKey, true)]
			[PropertyOrder(3)]
			public string Login
			{
				get { return (string)ExtensionInfo[nameof(Login)]; }
				set { ExtensionInfo[nameof(Login)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.PasswordKey)]
			[DescriptionLoc(LocalizedStrings.PasswordKey, true)]
			[PropertyOrder(4)]
			public SecureString Password
			{
				get { return ExtensionInfo[nameof(Password)].To<SecureString>(); }
				set { ExtensionInfo[nameof(Password)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.SecuritiesKey)]
			[DescriptionLoc(LocalizedStrings.Str2137Key, true)]
			[PropertyOrder(5)]
			[Editor(typeof(FileBrowserEditor), typeof(FileBrowserEditor))]
			public string SecurityCsvFile
			{
				get { return (string)ExtensionInfo[nameof(SecurityCsvFile)]; }
				set { ExtensionInfo[nameof(SecurityCsvFile)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.XamlStr23Key)]
			[DescriptionLoc(LocalizedStrings.OnlyActiveSecuritiesKey)]
			[PropertyOrder(6)]
			public bool OnlyActiveSecurities
			{
				get { return (bool)ExtensionInfo[nameof(OnlyActiveSecurities)]; }
				set { ExtensionInfo[nameof(OnlyActiveSecurities)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.GroupIdKey)]
			[DescriptionLoc(LocalizedStrings.GroupIdKey, true)]
			[PropertyOrder(7)]
			public char GroupId
			{
				get { return (char)ExtensionInfo[nameof(GroupId)]; }
				set { ExtensionInfo[nameof(GroupId)] = value; }
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

		public override HydraTaskSettings Settings => _settings;

		public override IEnumerable<DataType> SupportedDataTypes { get; } = new[]
		{
			DataType.Create(typeof(ExecutionMessage), ExecutionTypes.Tick),
			DataType.Create(typeof(ExecutionMessage), ExecutionTypes.OrderLog),
			DataType.Create(typeof(QuoteChangeMessage), null),
			DataType.Create(typeof(Level1ChangeMessage), null),
		};

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new ItchSettings(settings);

			if (!settings.IsDefault)
				return;

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

		protected override ItchMessageAdapter GetAdapter(IdGenerator generator)
		{
			return new ItchMessageAdapter(generator)
			{
				Login = _settings.Login,
				Password = _settings.Password,
				PrimaryMulticast = _settings.PrimaryMulticast,
				DuplicateMulticast = _settings.DuplicateMulticast,
				RecoveryAddress = _settings.RecoveryAddress,
				ReplayAddress = _settings.ReplayAddress,
				SecurityCsvFile = _settings.SecurityCsvFile,
				OnlyActiveSecurities = _settings.OnlyActiveSecurities,
				GroupId = _settings.GroupId
			};
		}
	}
}