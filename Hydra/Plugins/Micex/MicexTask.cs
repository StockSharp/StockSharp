#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Micex.MicexPublic
File: MicexTask.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Micex
{
	using System.Net;
	using System.Security;

	using Ecng.Common;
	using Ecng.Localization;
	using Ecng.Collections;
	using Ecng.ComponentModel;

	using StockSharp.Hydra.Core;
	using StockSharp.Messages;
	using StockSharp.Micex;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2281ParamsKey, _sourceName)]
	[TargetPlatform(Languages.Russian)]
	[Doc("http://stocksharp.com/doc/html/cb2a6b0f-ddf5-4a18-91f2-a460f2a9aa49.htm")]
	[TaskCategory(TaskCategories.Russia | TaskCategories.RealTime | TaskCategories.Forex |
		TaskCategories.Level1 | TaskCategories.MarketDepth | TaskCategories.Stock |
		TaskCategories.Transactions | TaskCategories.Paid | TaskCategories.Ticks)]
	class MicexTask : ConnectorHydraTask<MicexMessageAdapter>
	{
		private const string _sourceName = "Micex";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class MicexSettings : ConnectorHydraTaskSettings
		{
			public MicexSettings(HydraTaskSettings settings)
				: base(settings)
			{
				ExtensionInfo.TryAdd("OrderBookDepth", null);
				ExtensionInfo.TryAdd("RequestAllDepths", true);
				ExtensionInfo.TryAdd("MicexLogLevel", null);
				ExtensionInfo.TryAdd("OverrideDll", true);
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.AddressKey)]
			[DescriptionLoc(LocalizedStrings.AddressKey, true)]
			[PropertyOrder(0)]
			public EndPoint Address
			{
				get { return ExtensionInfo[nameof(Address)].To<EndPoint>(); }
				set { ExtensionInfo[nameof(Address)] = value.To<string>(); }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.LoginKey)]
			[DescriptionLoc(LocalizedStrings.LoginKey, true)]
			[PropertyOrder(1)]
			public string Login
			{
				get { return (string)ExtensionInfo[nameof(Login)]; }
				set { ExtensionInfo[nameof(Login)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.PasswordKey)]
			[DescriptionLoc(LocalizedStrings.PasswordKey, true)]
			[PropertyOrder(2)]
			public SecureString Password
			{
				get { return ExtensionInfo[nameof(Password)].To<SecureString>(); }
				set { ExtensionInfo[nameof(Password)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str3418Key)]
			[DescriptionLoc(LocalizedStrings.Str3419Key)]
			[PropertyOrder(3)]
			public string Interface
			{
				get { return (string)ExtensionInfo[nameof(Interface)]; }
				set { ExtensionInfo[nameof(Interface)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str3416Key)]
			[DescriptionLoc(LocalizedStrings.Str3692Key)]
			[PropertyOrder(4)]
			public string Server
			{
				get { return (string)ExtensionInfo[nameof(Server)]; }
				set { ExtensionInfo[nameof(Server)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str1197Key)]
			[DescriptionLoc(LocalizedStrings.Str1197Key, true)]
			[PropertyOrder(5)]
			public int? OrderBookDepth
			{
				get { return (int?)ExtensionInfo[nameof(OrderBookDepth)]; }
				set { ExtensionInfo[nameof(OrderBookDepth)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.AllDepthsKey)]
			[DescriptionLoc(LocalizedStrings.RequestAllDepthsKey)]
			[PropertyOrder(6)]
			public bool RequestAllDepths
			{
				get { return (bool)ExtensionInfo[nameof(RequestAllDepths)]; }
				set { ExtensionInfo[nameof(RequestAllDepths)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.LoggingKey)]
			[DescriptionLoc(LocalizedStrings.Str3422Key)]
			[PropertyOrder(7)]
			public string MicexLogLevel
			{
				get { return (string)ExtensionInfo[nameof(MicexLogLevel)]; }
				set { ExtensionInfo[nameof(MicexLogLevel)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.OverrideKey)]
			[DescriptionLoc(LocalizedStrings.OverrideDllKey)]
			[PropertyOrder(8)]
			public bool OverrideDll
			{
				get { return (bool)ExtensionInfo[nameof(OverrideDll)]; }
				set { ExtensionInfo[nameof(OverrideDll)] = value; }
			}

			[CategoryLoc(LocalizedStrings.GeneralKey)]
			[DisplayNameLoc(LocalizedStrings.Str2121Key)]
			[DescriptionLoc(LocalizedStrings.Str2121Key, true)]
			[PropertyOrder(16)]
			public string ExtraSettings
			{
				get { return (string)ExtensionInfo.TryGetValue(nameof(ExtraSettings)); }
				set { ExtensionInfo[nameof(ExtraSettings)] = value; }
			}
		}

		private MicexSettings _settings;

		public override HydraTaskSettings Settings => _settings;

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new MicexSettings(settings);

			if (!settings.IsDefault)
				return;

			_settings.Address = "127.0.0.1:8000".To<EndPoint>();
			_settings.Login = string.Empty;
			_settings.Password = new SecureString();
			_settings.Interface = MicexInterfaces.Stock22;
			_settings.Server = string.Empty;
			_settings.OrderBookDepth = null;
			_settings.RequestAllDepths = true;
			_settings.MicexLogLevel = null;
			_settings.OverrideDll = true;
			_settings.ExtraSettings = string.Empty;
		}

		protected override MicexMessageAdapter GetAdapter(IdGenerator generator)
		{
			return new MicexMessageAdapter(generator)
			{
				Login = _settings.Login,
				Password = _settings.Password,
				Interface = _settings.Interface,
				Server = _settings.Server,
				Addresses = new[] { _settings.Address },
				OrderBookDepth = _settings.OrderBookDepth,
				RequestAllDepths = _settings.RequestAllDepths,
				MicexLogLevel = _settings.MicexLogLevel,
				OverrideDll = _settings.OverrideDll,
				ExtraSettings = _settings.ExtraSettings
			};
		}
	}
}