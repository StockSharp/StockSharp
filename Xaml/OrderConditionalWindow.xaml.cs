namespace StockSharp.Xaml
{
	using System;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Common;
	using Ecng.Collections;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Окно для создания условной заявки.
	/// </summary>
	public partial class OrderConditionalWindow
	{
		/// <summary>
		/// Создать <see cref="OrderConditionalWindow"/>.
		/// </summary>
		public OrderConditionalWindow()
		{
			InitializeComponent();

			Order = new Order { Type = OrderTypes.Conditional };
		}

		/// <summary>
		/// Подключение к торговой системе.
		/// </summary>
		public IConnector Connector
		{
			get { return PortfolioCtrl.Connector; }
			set
			{
				PortfolioCtrl.Connector = value;

				if (SecurityProvider == null)
					SecurityProvider = new FilterableSecurityProvider(value);
			}
		}

		/// <summary>
		/// Поставщик информации об инструментах.
		/// </summary>
		public FilterableSecurityProvider SecurityProvider
		{
			get { return SecurityCtrl.SecurityProvider; }
			set { SecurityCtrl.SecurityProvider = value; }
		}

		private Order _order;

		/// <summary>
		/// Заявка.
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
					throw new ArgumentNullException("value");

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
				var connector = (Connector)Portfolio.Connector;

				var session = connector.TransactionAdapter.SessionHolder;

				var basketSession = session as BasketSessionHolder;
				if (basketSession != null)
					session = basketSession.Portfolios.TryGetValue(Portfolio.Name);

				Condition.SelectedObject = session == null ? null : session.CreateOrderCondition();
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