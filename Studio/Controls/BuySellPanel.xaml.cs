#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Controls.ControlsPublic
File: BuySellPanel.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License

namespace StockSharp.Studio.Controls
{
	using System;
	using System.Linq;
	using System.Windows;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Configuration;
	using Ecng.Serialization;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Studio.Core;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Xaml;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	public class BuySellSettings : NotifiableObject, IPersistable
	{
		private decimal _volume = 1;
		private bool _showLimitOrderPanel = true;
		private bool _showMarketOrderPanel = true;
		private bool _showCancelAll = true;
		private Portfolio _portfolio;
		private Security _security;
		private int _depth = 5;

		public event Action<Security, Security> SecurityChanged;

		[DisplayNameLoc(LocalizedStrings.SecurityKey)]
		[DescriptionLoc(LocalizedStrings.Str3192Key)]
		[PropertyOrder(10)]
		public Security Security
		{
			get { return _security; }
			set
			{
				if (_security == value)
					return;

				var oldValue = _security;
				_security = value;

				SecurityChanged.SafeInvoke(oldValue, value);
				NotifyChanged(nameof(Security));
			}
		}

		[DisplayNameLoc(LocalizedStrings.PortfolioKey)]
		[DescriptionLoc(LocalizedStrings.Str1997Key)]
		[PropertyOrder(15)]
		public Portfolio Portfolio
		{
			get { return _portfolio; }
			set
			{
				if (_portfolio == value)
					return;

				_portfolio = value;
				NotifyChanged(nameof(Portfolio));
			}
		}

		[DisplayNameLoc(LocalizedStrings.Str3193Key)]
		[DescriptionLoc(LocalizedStrings.Str3194Key)]
		[PropertyOrder(20)]
		public decimal Volume
		{
			get { return _volume; }
			set
			{
				_volume = value;
				NotifyChanged(nameof(Volume));
			}
		}

		[DisplayNameLoc(LocalizedStrings.Str1660Key)]
		[DescriptionLoc(LocalizedStrings.Str3195Key)]
		[PropertyOrder(21)]
		public int Depth
		{
			get { return _depth; }
			set
			{
				_depth = value;
				NotifyChanged(nameof(Depth));
			}
		}

		[DisplayNameLoc(LocalizedStrings.CancelAllKey)]
		[DescriptionLoc(LocalizedStrings.Str3196Key)]
		[PropertyOrder(30)]
		public bool ShowCancelAll
		{
			get { return _showCancelAll; }
			set
			{
				_showCancelAll = value;
				NotifyChanged(nameof(ShowCancelAll));
			}
		}

		[DisplayNameLoc(LocalizedStrings.Str3197Key)]
		[DescriptionLoc(LocalizedStrings.Str3198Key)]
		[PropertyOrder(40)]
		public bool ShowLimitOrderPanel
		{
			get { return _showLimitOrderPanel; }
			set
			{
				_showLimitOrderPanel = value;
				NotifyChanged(nameof(ShowLimitOrderPanel));
			}
		}

		[DisplayNameLoc(LocalizedStrings.MarketOrdersKey)]
		[DescriptionLoc(LocalizedStrings.Str3199Key)]
		[PropertyOrder(50)]
		public bool ShowMarketOrderPanel
		{
			get { return _showMarketOrderPanel; }
			set
			{
				_showMarketOrderPanel = value;
				NotifyChanged(nameof(ShowMarketOrderPanel));
			}
		}

		public void Load(SettingsStorage storage)
		{
			Volume = storage.GetValue(nameof(Volume), 1);
			ShowLimitOrderPanel = storage.GetValue(nameof(ShowLimitOrderPanel), true);
			ShowMarketOrderPanel = storage.GetValue(nameof(ShowMarketOrderPanel), true);
			ShowCancelAll = storage.GetValue(nameof(ShowCancelAll), true);
			Depth = storage.GetValue(nameof(Depth), MarketDepthControl.DefaultDepth);

			var secProvider = ConfigManager.GetService<ISecurityProvider>();

			var securityId = storage.GetValue<string>(nameof(Security));
			if (!securityId.IsEmpty())
			{
				if (securityId.CompareIgnoreCase("DEPTHSEC@FORTS"))
				{
					Security = "RI"
						.GetFortsJumps(DateTime.Today.AddMonths(3), DateTime.Today.AddMonths(6), code => secProvider.LookupById(code + "@" + ExchangeBoard.Forts.Code))
						.LastOrDefault();
				}
				else
					Security = secProvider.LookupById(securityId);
			}

			var pfProvider = ConfigManager.GetService<IPortfolioProvider>();

			var portfolioName = storage.GetValue<string>(nameof(Portfolio));
			if (!portfolioName.IsEmpty())
				Portfolio = pfProvider.Portfolios.FirstOrDefault(s => s.Name == portfolioName);
		}

		public void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Volume), Volume);
			storage.SetValue(nameof(ShowLimitOrderPanel), ShowLimitOrderPanel);
			storage.SetValue(nameof(ShowMarketOrderPanel), ShowMarketOrderPanel);
			storage.SetValue(nameof(ShowCancelAll), ShowCancelAll);
			storage.SetValue(nameof(Depth), Depth);
			//storage.SetValue(nameof(CreateOrderAtClick), CreateOrderAtClick);

			storage.SetValue(nameof(Security), Security?.Id);
			storage.SetValue(nameof(Portfolio), Portfolio?.Name);
		}
	}

	public partial class BuySellPanel : IStudioControl
	{
		private readonly BuySellSettings _settings = new BuySellSettings();

		public decimal LimitPrice { get; set; }

		public BuySellSettings Settings => _settings;

		public BuySellPanel()
		{
			InitializeComponent();

			_settings.PropertyChanged += (arg1, arg2) => new ControlChangedCommand(this).Process(this);

			SettingsPropertyGrid.SelectedObject = _settings;
			LimitPriceCtrl.Value = 0;
		}

		private Order CreateOrder(Sides direction, decimal price = 0, OrderTypes type = OrderTypes.Market)
		{
			return new Order
			{
				Portfolio = Settings.Portfolio,
				Security = Settings.Security,
				Direction = direction,
				Price = price,
				Volume = Settings.Volume,
				Type = type,
			};
		}

		private void BuyAtLimit_Click(object sender, RoutedEventArgs e)
		{
			var price = LimitPriceCtrl.Value;
			if(price == null || price <= 0)
				return;

			new RegisterOrderCommand(CreateOrder(Sides.Buy, price.Value, OrderTypes.Limit)).Process(this);
		}

		private void SellAtLimit_Click(object sender, RoutedEventArgs e)
		{
			var price = LimitPriceCtrl.Value;
			if(price == null || price <= 0)
				return;

			new RegisterOrderCommand(CreateOrder(Sides.Sell, price.Value, OrderTypes.Limit)).Process(this);
		}

		private void BuyAtMarket_Click(object sender, RoutedEventArgs e)
		{
			new RegisterOrderCommand(CreateOrder(Sides.Buy)).Process(this);
		}

		private void SellAtMarket_Click(object sender, RoutedEventArgs e)
		{
			new RegisterOrderCommand(CreateOrder(Sides.Sell)).Process(this);
		}

		private void ClosePosition_Click(object sender, RoutedEventArgs e)
		{
			new ClosePositionCommand(Settings.Security).Process(this);
		}

		private void RevertPosition_Click(object sender, RoutedEventArgs e)
		{
			new RevertPositionCommand(Settings.Security).Process(this);
		}

		private void CancelAll_Click(object sender, RoutedEventArgs e)
		{
			new CancelAllOrdersCommand().Process(this);
		}

		public void Save(SettingsStorage storage)
		{
			_settings.Save(storage);
		}

		public void Load(SettingsStorage storage)
		{
			_settings.Load(storage);
		}

		void IDisposable.Dispose()
		{
		}

		string IStudioControl.Title => string.Empty;

		Uri IStudioControl.Icon => null;
	}
}
