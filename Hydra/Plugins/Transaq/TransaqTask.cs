#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Transaq.TransaqPublic
File: TransaqTask.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Transaq
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Net;
	using System.Security;

	using Ecng.Collections;
	using Ecng.Localization;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Algo;
	using StockSharp.Messages;
	using StockSharp.Transaq;
	using StockSharp.Hydra.Core;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2281ParamsKey, _sourceName)]
	[TargetPlatform(Languages.Russian)]
	[Doc("http://stocksharp.com/doc/html/065a9dec-12d0-49d0-be8c-9f9b48f6a899.htm")]
	[TaskCategory(TaskCategories.Russia | TaskCategories.Transactions | TaskCategories.RealTime |
		TaskCategories.Candles | TaskCategories.Level1 | TaskCategories.MarketDepth |
		TaskCategories.Stock | TaskCategories.Free | TaskCategories.Ticks | TaskCategories.News)]
	class TransaqTask : ConnectorHydraTask<TransaqMessageAdapter>
	{
		private const string _sourceName = "Transaq";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class TransaqSettings : ConnectorHydraTaskSettings
		{
			public TransaqSettings(HydraTaskSettings settings)
				: base(settings)
			{
				ExtensionInfo.TryAdd("OverrideDll", true);
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.LoginKey)]
			[DescriptionLoc(LocalizedStrings.Str3679Key)]
			[PropertyOrder(0)]
			public string Login
			{
				get { return (string)ExtensionInfo[nameof(Login)]; }
				set { ExtensionInfo[nameof(Login)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.PasswordKey)]
			[DescriptionLoc(LocalizedStrings.Str3680Key)]
			[PropertyOrder(1)]
			public SecureString Password
			{
				get { return ExtensionInfo[nameof(Password)].To<SecureString>(); }
				set { ExtensionInfo[nameof(Password)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.AddressKey)]
			[DescriptionLoc(LocalizedStrings.Str3681Key)]
			[PropertyOrder(2)]
			public EndPoint Address
			{
				get { return ExtensionInfo[nameof(Address)].To<EndPoint>(); }
				set { ExtensionInfo[nameof(Address)] = value.To<string>(); }
			}

			[CategoryLoc(LocalizedStrings.Str3539Key)]
			[DisplayNameLoc(LocalizedStrings.Str3682Key)]
			[DescriptionLoc(LocalizedStrings.Str3683Key)]
			[PropertyOrder(3)]
			public bool UseProxy
			{
				get { return (bool)ExtensionInfo[nameof(UseProxy)]; }
				set { ExtensionInfo[nameof(UseProxy)] = value; }
			}

			[CategoryLoc(LocalizedStrings.Str3539Key)]
			[DisplayNameLoc(LocalizedStrings.Str3684Key)]
			[DescriptionLoc(LocalizedStrings.Str3685Key)]
			[PropertyOrder(4)]
			[ItemsSource(typeof(ProxyItemsSource))]
			public string ProxyType
			{
				get { return (string)ExtensionInfo[nameof(ProxyType)]; }
				set { ExtensionInfo[nameof(ProxyType)] = value; }
			}

			[CategoryLoc(LocalizedStrings.Str3539Key)]
			[DisplayNameLoc(LocalizedStrings.LoginKey)]
			[DescriptionLoc(LocalizedStrings.Str3686Key)]
			[PropertyOrder(5)]
			public string ProxyLogin
			{
				get { return (string)ExtensionInfo[nameof(ProxyLogin)]; }
				set { ExtensionInfo[nameof(ProxyLogin)] = value; }
			}

			[CategoryLoc(LocalizedStrings.Str3539Key)]
			[DisplayNameLoc(LocalizedStrings.PasswordKey)]
			[DescriptionLoc(LocalizedStrings.Str3687Key)]
			[PropertyOrder(6)]
			public SecureString ProxyPassword
			{
				get { return ExtensionInfo[nameof(ProxyPassword)].To<SecureString>(); }
				set { ExtensionInfo[nameof(ProxyPassword)] = value; }
			}

			[CategoryLoc(LocalizedStrings.Str3539Key)]
			[DisplayNameLoc(LocalizedStrings.AddressKey)]
			[DescriptionLoc(LocalizedStrings.Str3688Key)]
			[PropertyOrder(7)]
			public EndPoint ProxyAddress
			{
				get { return ExtensionInfo[nameof(ProxyAddress)].To<EndPoint>(); }
				set { ExtensionInfo[nameof(ProxyAddress)] = value.To<string>(); }
			}

			[CategoryLoc(_sourceName)]
			[DisplayName("HFT")]
			[DescriptionLoc(LocalizedStrings.Str3545Key)]
			[PropertyOrder(8)]
			public bool IsHFT
			{
				get { return (bool)ExtensionInfo[nameof(IsHFT)]; }
				set { ExtensionInfo[nameof(IsHFT)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str736Key)]
			[DescriptionLoc(LocalizedStrings.Str3547Key)]
			[PropertyOrder(9)]
			public TimeSpan? MarketDataInterval
			{
				get { return (TimeSpan?)ExtensionInfo[nameof(MarketDataInterval)]; }
				set { ExtensionInfo[nameof(MarketDataInterval)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.OverrideKey)]
			[DescriptionLoc(LocalizedStrings.OverrideDllKey)]
			[PropertyOrder(10)]
			public bool OverrideDll
			{
				get { return (bool)ExtensionInfo[nameof(OverrideDll)]; }
				set { ExtensionInfo[nameof(OverrideDll)] = value; }
			}

			[Browsable(true)]
			public override bool IsDownloadNews
			{
				get { return base.IsDownloadNews; }
				set { base.IsDownloadNews = value; }
			}
		}

		public TransaqTask()
		{
			SupportedDataTypes = new[]
			{
				TimeSpan.FromMinutes(1),
				TimeSpan.FromHours(1),
				TimeSpan.FromDays(1)
			}
			.Select(tf => DataType.Create(typeof(TimeFrameCandleMessage), tf))
			.Concat(base.SupportedDataTypes)
			.ToArray();
		}

		private TransaqSettings _settings;

		public override HydraTaskSettings Settings => _settings;

		public override IEnumerable<DataType> SupportedDataTypes { get; }

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new TransaqSettings(settings);

			if (!settings.IsDefault)
				return;

			_settings.Login = string.Empty;
			_settings.Password = new SecureString();
			_settings.Address = TransaqAddresses.FinamReal1;
			_settings.IsHFT = false;
			_settings.MarketDataInterval = null;
			_settings.IsDownloadNews = true;
			_settings.OverrideDll = true;

			_settings.UseProxy = false;
			_settings.ProxyType = ProxyTypes.Http.To<string>();
			_settings.ProxyLogin = string.Empty;
			_settings.ProxyPassword = new SecureString();
			_settings.ProxyAddress = new IPEndPoint(IPAddress.Loopback, 8080);
		}

		protected override TransaqMessageAdapter GetAdapter(IdGenerator generator)
		{
			var adapter = new TransaqMessageAdapter(generator)
			{
				Login = _settings.Login,
				Password = _settings.Password,
				Address = _settings.Address,
				IsHFT = _settings.IsHFT,
				MarketDataInterval = _settings.MarketDataInterval,
				OverrideDll = _settings.OverrideDll,
			};

			if (_settings.UseProxy)
			{
				adapter.Proxy = new Proxy
				{
					Login = _settings.ProxyLogin,
					Password = _settings.ProxyPassword.To<string>(),
					Address = _settings.ProxyAddress,
					Type = _settings.ProxyType.To<ProxyTypes>()
				};
			}

			return adapter;
		}
	}
}