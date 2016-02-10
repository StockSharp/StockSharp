#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: OrderWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The window for the order creating.
	/// </summary>
	public partial class OrderWindow
	{
		private enum OrderWindowTif
		{
			[EnumDisplayNameLoc(LocalizedStrings.GTCKey)]
			Gtc,

			[EnumDisplayNameLoc(LocalizedStrings.FOKKey)]
			MatchOrCancel,

			[EnumDisplayNameLoc(LocalizedStrings.IOCKey)]
			CancelBalance,

			[EnumDisplayNameLoc(LocalizedStrings.SessionKey)]
			Today,

			[EnumDisplayNameLoc(LocalizedStrings.GTDKey)]
			Gtd,
		}

		private class SecurityData : NotifiableObject
		{
			private decimal? _maxPrice;
			private decimal? _minPrice;
			private decimal? _lastTradePrice;
			private decimal? _bestAskPrice;
			private decimal? _bestBidPrice;

			public decimal? BestBidPrice
			{
				get { return _bestBidPrice; }
				set
				{
					_bestBidPrice = value;
					NotifyChanged(nameof(BestBidPrice));
				}
			}

			public decimal? BestAskPrice
			{
				get { return _bestAskPrice; }
				set
				{
					_bestAskPrice = value;
					NotifyChanged(nameof(BestAskPrice));
				}
			}

			public decimal? LastTradePrice
			{
				get { return _lastTradePrice; }
				set
				{
					_lastTradePrice = value;
					NotifyChanged(nameof(LastTradePrice));
				}
			}

			public decimal? MinPrice
			{
				get { return _minPrice; }
				set
				{
					_minPrice = value;
					NotifyChanged(nameof(MinPrice));
				}
			}

			public decimal? MaxPrice
			{
				get { return _maxPrice; }
				set
				{
					_maxPrice = value;
					NotifyChanged(nameof(MaxPrice));
				}
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="OrderWindow"/>.
		/// </summary>
		public OrderWindow()
		{
			InitializeComponent();

			TimeInForceCtrl.SetDataSource<OrderWindowTif>();

			Order = new Order { Type = OrderTypes.Limit };
			DataContext = Data = new SecurityData();
		}

		private SecurityData Data { get; }

		/// <summary>
		/// Available portfolios.
		/// </summary>
		public ThreadSafeObservableCollection<Portfolio> Portfolios
		{
			get { return PortfolioCtrl.Portfolios; }
			set { PortfolioCtrl.Portfolios = value; }
		}

		private IMarketDataProvider _marketDataProvider;

		/// <summary>
		/// The market data provider.
		/// </summary>
		public IMarketDataProvider MarketDataProvider
		{
			get { return _marketDataProvider; }
			set
			{
				if (value == _marketDataProvider)
					return;

				if (_marketDataProvider != null)
					_marketDataProvider.ValuesChanged -= MarketDataProviderOnValuesChanged;

				_marketDataProvider = value;

				if (_marketDataProvider == null)
					return;

				if (Security != null)
					FillDataDefaults();

				_marketDataProvider.ValuesChanged += MarketDataProviderOnValuesChanged;
			}
		}

		/// <summary>
		/// The provider of information about instruments.
		/// </summary>
		public ISecurityProvider SecurityProvider
		{
			get { return SecurityCtrl.SecurityProvider; }
			set { SecurityCtrl.SecurityProvider = value; }
		}

		private Order _order;

		/// <summary>
		/// Order.
		/// </summary>
		public Order Order
		{
			get
			{
				_order.Type = IsMarketCtrl.IsChecked == true ? OrderTypes.Market : OrderTypes.Limit;
				_order.Security = Security;
				_order.Portfolio = Portfolio;
				_order.ClientCode = ClientCodeCtrl.Text;
				_order.Price = PriceCtrl.Value ?? 0;
				_order.Volume = VolumeCtrl.Value ?? 0;
				_order.VisibleVolume = VisibleVolumeCtrl.Value;
				_order.Direction = IsBuyCtrl.IsChecked == true ? Sides.Buy : Sides.Sell;
				_order.Comment = CommentCtrl.Text;

				switch ((OrderWindowTif?)TimeInForceCtrl.SelectedValue)
				{
					case OrderWindowTif.MatchOrCancel:
						_order.TimeInForce = TimeInForce.MatchOrCancel;
						break;
					case OrderWindowTif.CancelBalance:
						_order.TimeInForce = TimeInForce.CancelBalance;
						break;
					case OrderWindowTif.Gtc:
						_order.TimeInForce = TimeInForce.PutInQueue;
						break;
					case OrderWindowTif.Today:
						_order.TimeInForce = TimeInForce.PutInQueue;
						_order.ExpiryDate = DateTime.Today.ApplyTimeZone(Security.Board.TimeZone);
						break;
					case OrderWindowTif.Gtd:
						_order.TimeInForce = TimeInForce.PutInQueue;
						_order.ExpiryDate = (ExpiryDate.Value ?? DateTime.Today).ApplyTimeZone(Security.Board.TimeZone);
						break;
					case null:
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				return _order;
			}
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_order = value;

				switch (value.Type)
				{
					case OrderTypes.Limit:
					case null:
						IsMarketCtrl.IsChecked = false;
						break;
					case OrderTypes.Market:
						IsMarketCtrl.IsChecked = true;
						break;
					case OrderTypes.Conditional:
					case OrderTypes.Repo:
					case OrderTypes.ExtRepo:
					case OrderTypes.Rps:
					case OrderTypes.Execute:
						throw new NotSupportedException(LocalizedStrings.Str2872Params.Put(value.Type));
					default:
						throw new ArgumentOutOfRangeException();
				}

				Security = value.Security;
				Portfolio = value.Portfolio;
				ClientCodeCtrl.Text = value.ClientCode;
				PriceCtrl.Value = value.Price == 0 ? PriceCtrl.Increment : value.Price;
				VolumeCtrl.Value = value.Volume == 0 ? VolumeCtrl.Increment : value.Volume;
				VisibleVolumeCtrl.Value = value.VisibleVolume;
				IsBuyCtrl.IsChecked = value.Direction == Sides.Buy;
				IsSellCtrl.IsChecked = value.Direction == Sides.Sell;
				CommentCtrl.Text = value.Comment;

				switch (value.TimeInForce)
				{
					case null:
					case TimeInForce.PutInQueue:
					{
						if (value.ExpiryDate == null || value.ExpiryDate == DateTimeOffset.MaxValue)
							TimeInForceCtrl.SelectedValue = OrderWindowTif.Gtc;
						else if (value.ExpiryDate == DateTimeOffset.Now.Date.ApplyTimeZone(Security.Board.TimeZone))
							TimeInForceCtrl.SelectedValue = OrderWindowTif.Today;
						else
						{
							TimeInForceCtrl.SelectedValue = OrderWindowTif.Gtd;
							ExpiryDate.Value = value.ExpiryDate.Value.Date;
							//throw new ArgumentOutOfRangeException("value", value.ExpiryDate, LocalizedStrings.Str1541);
						}

						break;
					}
					case TimeInForce.MatchOrCancel:
						TimeInForceCtrl.SelectedValue = OrderWindowTif.MatchOrCancel;
						break;
					case TimeInForce.CancelBalance:
						TimeInForceCtrl.SelectedValue = OrderWindowTif.CancelBalance;
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				if (Security != null && !Security.Board.IsSupportMarketOrders)
					IsMarketCtrl.IsEnabled = false;

				if (_order.State != OrderStates.None)
				{
					IsMarketCtrl.IsEnabled = PortfolioCtrl.IsEnabled = SecurityCtrl.IsEnabled = false;
				}
			}
		}

		private Security Security
		{
			get { return SecurityCtrl.SelectedSecurity; }
			set { SecurityCtrl.SelectedSecurity = value; }
		}

		private Portfolio Portfolio
		{
			get { return PortfolioCtrl.SelectedPortfolio; }
			set { PortfolioCtrl.SelectedPortfolio = value; }
		}

		private void PortfolioCtrl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			TryEnableSend();
		}

		private void SecurityCtrl_OnSecuritySelected()
		{
			TryEnableSend();

			var isNull = Security == null;

			PriceCtrl.Increment = isNull ? 0.01m : Security.PriceStep ?? 1m;
			VolumeCtrl.Increment = isNull ? 1m : Security.VolumeStep ?? 1m;

			MinPrice.IsEnabled = MaxPrice.IsEnabled = BestBidPrice.IsEnabled = BestAskPrice.IsEnabled
				= LastTradePrice.IsEnabled = !isNull;

			Data.BestBidPrice = Data.BestAskPrice = Data.LastTradePrice = Data.MinPrice = Data.MaxPrice = null;

			if (isNull || MarketDataProvider == null)
				return;

			FillDataDefaults();
		}

		private void FillDataDefaults()
		{
			Data.BestBidPrice = GetSecurityValue(Level1Fields.BestBidPrice);
			Data.BestAskPrice = GetSecurityValue(Level1Fields.BestAskPrice);
			Data.LastTradePrice = GetSecurityValue(Level1Fields.LastTradePrice);
			Data.MinPrice = GetSecurityValue(Level1Fields.MinPrice);
			Data.MaxPrice = GetSecurityValue(Level1Fields.MaxPrice);
		}

		private decimal? GetSecurityValue(Level1Fields field)
		{
			return (decimal?)MarketDataProvider.GetSecurityValue(Security, field);
		}

		private void IsMarketCtrl_OnClick(object sender, RoutedEventArgs e)
		{
			TryEnableSend();
		}

		private void VolumeCtrl_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			UpdateAmount();
			TryEnableSend();
		}

		private void PriceCtrl_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			UpdateAmount();
			TryEnableSend();
		}

		private void TryEnableSend()
		{
			Send.IsEnabled = Security != null && Portfolio != null && VolumeCtrl.Value > 0
				&& (IsMarketCtrl.IsChecked == true || PriceCtrl.Value > 0)
				&& (ExpiryDate.IsEnabled != true || ExpiryDate.Value != null);
		}

		private void MarketDataProviderOnValuesChanged(Security security, IEnumerable<KeyValuePair<Level1Fields, object>> changes, DateTimeOffset serverTime, DateTimeOffset localTime)
		{
			if (Security != security)
				return;

			foreach (var change in changes)
			{
				switch (change.Key)
				{
					case Level1Fields.BestAskPrice:
						Data.BestAskPrice = (decimal)change.Value;
						break;
					case Level1Fields.BestBidPrice:
						Data.BestBidPrice = (decimal)change.Value;
						break;
					case Level1Fields.LastTradePrice:
						Data.LastTradePrice = (decimal)change.Value;
						break;
					case Level1Fields.MinPrice:
						Data.MinPrice = (decimal)change.Value;
						break;
					case Level1Fields.MaxPrice:
						Data.MaxPrice = (decimal)change.Value;
						break;
				}
			}
		}

		private void MaxPrice_OnClick(object sender, RoutedEventArgs e)
		{
			if (Data.MaxPrice == null)
				return;

			PriceCtrl.Value = Data.MaxPrice;
			IsMarketCtrl.IsChecked = false;
		}

		private void MinPrice_OnClick(object sender, RoutedEventArgs e)
		{
			if (Data.MinPrice == null)
				return;

			PriceCtrl.Value = Data.MinPrice;
			IsMarketCtrl.IsChecked = false;
		}

		private void LastTradePrice_OnClick(object sender, RoutedEventArgs e)
		{
			var trade = Data.LastTradePrice;

			if (trade == null)
				return;

			PriceCtrl.Value = trade;
			IsMarketCtrl.IsChecked = false;
		}

		private void BestBidPrice_OnClick(object sender, RoutedEventArgs e)
		{
			var bid = Data.BestBidPrice;

			if (bid == null)
				return;

			PriceCtrl.Value = bid;
			IsMarketCtrl.IsChecked = false;
		}

		private void BestAskPrice_OnClick(object sender, RoutedEventArgs e)
		{
			var ask = Data.BestAskPrice;

			if (ask == null)
				return;

			PriceCtrl.Value = ask;
			IsMarketCtrl.IsChecked = false;
		}

		private void Vol1_OnClick(object sender, RoutedEventArgs e)
		{
			VolumeCtrl.Value = 1;
		}

		private void Vol10_OnClick(object sender, RoutedEventArgs e)
		{
			VolumeCtrl.Value = 10;
		}

		private void Vol20_OnClick(object sender, RoutedEventArgs e)
		{
			VolumeCtrl.Value = 20;
		}

		private void Vol50_OnClick(object sender, RoutedEventArgs e)
		{
			VolumeCtrl.Value = 50;
		}

		private void Vol100_OnClick(object sender, RoutedEventArgs e)
		{
			VolumeCtrl.Value = 100;
		}

		private void Vol200_OnClick(object sender, RoutedEventArgs e)
		{
			VolumeCtrl.Value = 200;
		}

		private bool _fromAmount;

		private void ToVolume_OnClick(object sender, RoutedEventArgs e)
		{
			var amount = AmountCtrl.Value;

			if (amount == null)
				return;

			var price = GetPrice();
			var volume = VolumeCtrl.Value;

			_fromAmount = true;

			try
			{
				if (price == 0)
				{
					if (volume == 0 || volume == null)
						return;

					var step = PriceCtrl.Increment ?? 0.01m;
					PriceCtrl.Value = MathHelper.Round(amount.Value / volume.Value, step, step.GetCachedDecimals());
				}
				else
				{
					var step = VolumeCtrl.Increment ?? 1m;
					VolumeCtrl.Value = MathHelper.Round(amount.Value / price, step, step.GetCachedDecimals());
				}
			}
			finally
			{
				_fromAmount = false;
			}
		}

		private decimal GetPrice()
		{
			return PriceCtrl.Value ?? (Data.LastTradePrice ?? 0);
		}

		private void UpdateAmount()
		{
			if (_fromAmount)
				return;

			AmountCtrl.Value = GetPrice() * VolumeCtrl.Value;
		}

		private void AmountCtrl_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			ToVolume.IsEnabled = AmountCtrl.Value != null;
		}

		private void TimeInForceCtrl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var member = e.AddedItems.Cast<EnumComboBoxHelper.EnumerationMember>().FirstOrDefault();

			ExpiryDate.IsEnabled = member != null && (OrderWindowTif?)member.Value == OrderWindowTif.Gtd;
			TryEnableSend();
		}

		private void ExpiryDate_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			TryEnableSend();
		}
	}
}