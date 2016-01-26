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

	using ActiproSoftware.Windows.Controls.Editors;

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

	public class BuySellSettings : NotifiableObject
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
				NotifyChanged("Security");
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
				NotifyChanged("Portfolio");
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
				NotifyChanged("Volume");
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
				NotifyChanged("Depth");
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
				NotifyChanged("ShowCancelAll");
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
				NotifyChanged("ShowLimitOrderPanel");
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
				NotifyChanged("ShowMarketOrderPanel");
			}
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
			LimitPriceCtrl.Text = string.Empty;
		}

		private bool CheckLimitPrice()
		{
			var expression = LimitPriceCtrl.GetBindingExpression(MaskedTextBox.TextProperty);
			if (expression != null)
			{
				expression.UpdateSource();

				return !expression.HasError;
			}

			return false;
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
			if (!CheckLimitPrice())
				return;

			new RegisterOrderCommand(CreateOrder(Sides.Buy, LimitPrice, OrderTypes.Limit)).Process(this);
		}

		private void SellAtLimit_Click(object sender, RoutedEventArgs e)
		{
			if (!CheckLimitPrice())
				return;

            //TODO: регистрация команды в IComandService
			new RegisterOrderCommand(CreateOrder(Sides.Sell, LimitPrice, OrderTypes.Limit)).Process(this);
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
			new ClosePositionCommand().Process(this);
		}

		private void RevertPosition_Click(object sender, RoutedEventArgs e)
		{
			new RevertPositionCommand().Process(this);
		}

		private void CancelAll_Click(object sender, RoutedEventArgs e)
		{
			new CancelAllOrdersCommand().Process(this);
		}

		public void Save(SettingsStorage settings)
		{
			settings.SetValue("Volume", _settings.Volume);
			settings.SetValue("ShowLimitOrderPanel", _settings.ShowLimitOrderPanel);
			settings.SetValue("ShowMarketOrderPanel", _settings.ShowMarketOrderPanel);
			settings.SetValue("ShowCancelAll", _settings.ShowCancelAll);
			settings.SetValue("Depth", _settings.Depth);
			//settings.SetValue("CreateOrderAtClick", _settings.CreateOrderAtClick);

			settings.SetValue("Security", _settings.Security != null ? _settings.Security.Id : null);
			settings.SetValue("Portfolio", _settings.Portfolio != null ? _settings.Portfolio.Name : null);
		}

		public void Load(SettingsStorage settings)
		{
			_settings.Volume = settings.GetValue("Volume", 1);
			_settings.ShowLimitOrderPanel = settings.GetValue("ShowLimitOrderPanel", true);
			_settings.ShowMarketOrderPanel = settings.GetValue("ShowMarketOrderPanel", true);
			_settings.ShowCancelAll = settings.GetValue("ShowCancelAll", true);
			_settings.Depth = settings.GetValue("Depth", MarketDepthControl.DefaultDepth);

			var connector = ConfigManager.GetService<IConnector>();

			var securityId = settings.GetValue<string>("Security");
			if (!securityId.IsEmpty())
			{
				if (securityId.CompareIgnoreCase("DEPTHSEC@FORTS"))
				{
					_settings.Security = "RI"
						.GetFortsJumps(DateTime.Today.AddMonths(3), DateTime.Today.AddMonths(6), code => connector.LookupById(code + "@" + ExchangeBoard.Forts.Code))
						.LastOrDefault();
				}
				else
					_settings.Security = connector.LookupById(securityId);
			}

			var portfolioName = settings.GetValue<string>("Portfolio");
			if (!portfolioName.IsEmpty())
				_settings.Portfolio = connector.Portfolios.FirstOrDefault(s => s.Name == portfolioName);
		}

		void IDisposable.Dispose()
		{
		}

		string IStudioControl.Title => string.Empty;

		Uri IStudioControl.Icon => null;
	}
}
