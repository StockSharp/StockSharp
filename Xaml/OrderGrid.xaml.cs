namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using System.Windows.Controls;
	using System.Windows.Data;
	using System.Windows.Input;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Таблица, отображающая заявки (<see cref="Order"/>).
	/// </summary>
	public partial class OrderGrid
	{
		/// <summary>
		/// Команда на регистрацию заявки.
		/// </summary>
		public static RoutedCommand RegisterOrderCommand = new RoutedCommand();

		/// <summary>
		/// Команда на перерегистрацию заявки.
		/// </summary>
		public static RoutedCommand ReRegisterOrderCommand = new RoutedCommand();

		/// <summary>
		/// Команда на отмену выбранных заявок.
		/// </summary>
		public static RoutedCommand CancelOrderCommand = new RoutedCommand();

		/// <summary>
		/// Команда на копирование текста ошибки.
		/// </summary>
		public static RoutedCommand CopyErrorCommand = new RoutedCommand();

		private class OrderItem : NotifiableObject
		{
			private Order _order;

			public Order Order
			{
				get { return _order; }
				set
				{
					_order = value;
					NotifyChanged("Order");
				}
			}

			private string _comment;

			public string Comment
			{
				get { return _comment; }
				set
				{
					_comment = value;
					NotifyChanged("Comment");
				}
			}

			private string _condition;

			public string Condition
			{
				get { return _condition; }
				set
				{
					_condition = value;
					NotifyChanged("Condition");
				}
			}
		}

		private readonly ConvertibleObservableCollection<Order, OrderItem> _orders;

		/// <summary>
		/// Создать <see cref="OrderGrid"/>.
		/// </summary>
		public OrderGrid()
		{
			InitializeComponent();

			var itemsSource = new ObservableCollectionEx<OrderItem>();
			ItemsSource = itemsSource;

			_orders = new ConvertibleObservableCollection<Order, OrderItem>(new ThreadSafeObservableCollection<OrderItem>(itemsSource), order => new OrderItem
			{
				Order = order,
				Condition = FormatCondition(order.Condition),
			}) { MaxCount = 100000 };

			ContextMenu.Items.Add(new Separator());
			ContextMenu.Items.Add(new MenuItem { Header = LocalizedStrings.Str1566, Command = RegisterOrderCommand, CommandTarget = this});
		}

		/// <summary>
		/// Максимальное число заявок для показа. Значение -1 означает бесконечное количество.
		/// По-умолчанию равно 100000.
		/// </summary>
		public int MaxCount
		{
			get { return _orders.MaxCount; }
			set { _orders.MaxCount = value; }
		}

		/// <summary>
		/// Список заявок, добавленных в таблицу.
		/// </summary>
		public IListEx<Order> Orders
		{
			get { return _orders; }
		}

		/// <summary>
		/// Добавить описание ошибки регистрации в таблицу.
		/// </summary>
		/// <param name="fail">Ошибка.</param>
		public void AddRegistrationFail(OrderFail fail)
		{
			if (fail == null)
				throw new ArgumentNullException("fail");

			var item = _orders.TryGet(fail.Order);

			if (item != null)
				item.Comment = fail.Error.Message;
		}

		/// <summary>
		/// Выбранная заявка.
		/// </summary>
		public Order SelectedOrder
		{
			get { return SelectedOrders.FirstOrDefault(); }
		}

		/// <summary>
		/// Выбранные заявки.
		/// </summary>
		public IEnumerable<Order> SelectedOrders
		{
			get { return SelectedItems.Cast<OrderItem>().Select(i => i.Order); }
		}

		/// <summary>
		/// Событие ререгистрации заявки.
		/// </summary>
		public event Action OrderRegistering;

		/// <summary>
		/// Событие перерегистрации заявки.
		/// </summary>
		public event Action<Order> OrderReRegistering;

		/// <summary>
		/// Событие отмены выбранных заявок.
		/// </summary>
		public event Action<IEnumerable<Order>> OrderCanceling;

		/// <summary>
		/// Метод вызывается при добавлении новой заявки.
		/// </summary>
		/// <param name="order">Заявка.</param>
		protected virtual void OnOrderAdded(Order order)
		{
		}

		private void ExecutedRegisterOrderCommand(object sender, ExecutedRoutedEventArgs e)
		{
			OrderRegistering.SafeInvoke();
		}

		private void CanExecuteRegisterOrderCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = OrderRegistering != null;
		}

		private void ExecutedReRegisterOrder(object sender, ExecutedRoutedEventArgs e)
		{
			OrderReRegistering.SafeInvoke(SelectedOrder);
		}

		private void CanExecuteReRegisterOrder(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = OrderReRegistering != null && SelectedOrder != null && SelectedOrder.State == OrderStates.Active;
		}

		private void ExecutedCancelOrder(object sender, ExecutedRoutedEventArgs e)
		{
			OrderCanceling.SafeInvoke(SelectedOrders);
		}

		private void CanExecuteCancelOrder(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = OrderCanceling != null && SelectedOrders.Any(o => o.State == OrderStates.Active);
		}

		internal static string FormatCondition(OrderCondition condition)
		{
			return condition == null
				? string.Empty
				: condition.Parameters.Select(p => "{Key} = {Value}".PutEx(p)).Join(Environment.NewLine);
		}

		private void ExecutedCopyErrorCommand(object sender, ExecutedRoutedEventArgs e)
		{
			e.Parameter.CopyToClipboard();
			e.Handled = true;
		}
	}

	class OrderStateConverter : IMultiValueConverter
	{
		object IMultiValueConverter.Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			var state = (OrderStates)values[0];
			var order = (Order)values[1];

			switch (state)
			{
				case OrderStates.None:
					return "--";
				case OrderStates.Pending:
					return LocalizedStrings.Str538;
				case OrderStates.Failed:
					return LocalizedStrings.Str152;
				case OrderStates.Active:
					return LocalizedStrings.Str238;
				case OrderStates.Done:
					return order.IsMatched() ? LocalizedStrings.Str1328 : LocalizedStrings.Str1329;
			}

			return state;
		}

		object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}

	class OrderTimeInForceConverter : IMultiValueConverter
	{
		object IMultiValueConverter.Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			var tif = (TimeInForce)values[0];
			var expiryDate = (DateTimeOffset)values[1];

			switch (tif)
			{
				case TimeInForce.PutInQueue:
				{
					if (expiryDate == DateTimeOffset.MaxValue)
						return "GTC";
					else if (expiryDate == DateTimeOffset.Now.Date)
						return "GTD";
					else
						return expiryDate.LocalDateTime.ToString("d");
				}
				case TimeInForce.MatchOrCancel:
					return "FOK";
				case TimeInForce.CancelBalance:
					return "IOC";
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}

	class OrderConditionConverter : IValueConverter
	{
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return OrderGrid.FormatCondition((OrderCondition)value);
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}