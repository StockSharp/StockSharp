#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: OrderConditionalWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The window for the conditional order creating.
	/// </summary>
	public partial class OrderConditionalWindow
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="OrderConditionalWindow"/>.
		/// </summary>
		public OrderConditionalWindow()
		{
			InitializeComponent();

			Order = new Order { Type = OrderTypes.Conditional };
		}

		/// <summary>
		/// Available portfolios.
		/// </summary>
		public ThreadSafeObservableCollection<Portfolio> Portfolios
		{
			get { return PortfolioCtrl.Portfolios; }
			set { PortfolioCtrl.Portfolios = value; }
		}

		/// <summary>
		/// The provider of information about instruments.
		/// </summary>
		public ISecurityProvider SecurityProvider
		{
			get { return SecurityCtrl.SecurityProvider; }
			set { SecurityCtrl.SecurityProvider = value; }
		}

		/// <summary>
		/// The market data provider.
		/// </summary>
		public IMarketDataProvider MarketDataProvider { get; set; } // reserved for future

		/// <summary>
		/// The message adapter.
		/// </summary>
		public IMessageAdapter Adapter { get; set; }

		private Order _order;

		/// <summary>
		/// Order.
		/// </summary>
		public Order Order
		{
			get
			{
				_order.Security = Security;
				_order.Portfolio = Portfolio;
				_order.Price = PriceCtrl.Value ?? 0;
				_order.Volume = VolumeCtrl.Value ?? 0;
				_order.Direction = IsBuyCtrl.IsChecked == true ? Sides.Buy : Sides.Sell;
				_order.Condition = (OrderCondition)Condition.SelectedObject;

				return _order;
			}
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				if (value.Type != OrderTypes.Conditional)
					throw new NotSupportedException(LocalizedStrings.Str2872Params.Put(value.Type));

				_order = value;

				Security = value.Security;
				Portfolio = value.Portfolio;
				VolumeCtrl.Value = value.Volume;
				PriceCtrl.Value = value.Price;
				IsBuyCtrl.IsChecked = value.Direction == Sides.Buy;
				Condition.SelectedObject = value.Condition;

				if (value.Condition == null && value.Portfolio != null)
					CreateCondition();
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

		private void CreateCondition()
		{
			if (Portfolio == null)
				Condition.SelectedObject = null;
			else
			{
				var adapter = Adapter;

				var basketAdapter = adapter as BasketMessageAdapter;
				if (basketAdapter != null)
					adapter = basketAdapter.Portfolios.TryGetValue(Portfolio.Name);

				Condition.SelectedObject = adapter == null ? null : adapter.CreateOrderCondition();
			}
		}

		private void PortfolioCtrl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			CreateCondition();
			TryEnableSend();
		}

		private void SecurityCtrl_OnSecuritySelected()
		{
			TryEnableSend();
		}

		private void VolumeCtrl_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			TryEnableSend();
		}

		private void Condition_OnSelectedObjectChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			TryEnableSend();
		}

		private void TryEnableSend()
		{
			Send.IsEnabled = Security != null && Portfolio != null && VolumeCtrl.Value > 0 && Condition.SelectedObject != null;
		}
	}
}