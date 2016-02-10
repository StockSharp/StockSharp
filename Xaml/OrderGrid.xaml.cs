#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: OrderGrid.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
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
	/// The table showing orders (<see cref="Order"/>).
	/// </summary>
	public partial class OrderGrid
	{
		/// <summary>
		/// The command for the order registration.
		/// </summary>
		public static RoutedCommand RegisterOrderCommand = new RoutedCommand();

		/// <summary>
		/// The command for the order re-registration.
		/// </summary>
		public static RoutedCommand ReRegisterOrderCommand = new RoutedCommand();

		/// <summary>
		/// The command for the cancel of selected orders.
		/// </summary>
		public static RoutedCommand CancelOrderCommand = new RoutedCommand();

		/// <summary>
		/// The command for the copying of the error text.
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
					if (_order == value)
						return;

					if (_order != null)
						_order.PropertyChanged -= OrderOnPropertyChanged;

					_order = value;

					if (_order != null)
						_order.PropertyChanged += OrderOnPropertyChanged;

					NotifyChanged(nameof(Order));
				}
			}

			private string _comment;

			public string Comment
			{
				get { return _comment; }
				set
				{
					_comment = value;
					NotifyChanged(nameof(Comment));
				}
			}

			private string _condition;

			public string Condition
			{
				get { return _condition; }
				set
				{
					_condition = value;
					NotifyChanged(nameof(Condition));
				}
			}

			public string OrderId
			{
				get
				{
					var order = Order;

					if (order == null)
						return null;

					return order.Id.To<string>() ?? order.StringId;
				}
			}

			public int OrderState
			{
				get
				{
					var order = Order;

					if (order == null)
						return -1;

					switch (order.State)
					{
						case OrderStates.None:
							return 0;
						case OrderStates.Pending:
							return 1;
						case OrderStates.Failed:
							return 2;
						case OrderStates.Active:
							return 3;
						case OrderStates.Done:
							return order.IsMatched() ? 4 : 5;
						default:
							throw new InvalidOperationException(LocalizedStrings.Str1596Params.Put(order.State, order));
					}
				}
			}

			public long OrderTif
			{
				get
				{
					var order = Order;

					if (order == null)
						return -1;

					var tif = order.TimeInForce;
					var expiryDate = order.ExpiryDate;

					switch (tif)
					{
						case null:
						case TimeInForce.PutInQueue:
							return expiryDate?.Ticks ?? long.MaxValue;
						case TimeInForce.MatchOrCancel:
							return 0;
						case TimeInForce.CancelBalance:
							return 1;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}
			}

			private void OrderOnPropertyChanged(object sender, PropertyChangedEventArgs e)
			{
				if (e.PropertyName == nameof(Order.Id) || e.PropertyName == nameof(Order.StringId))
					NotifyChanged(nameof(OrderId));

				if (e.PropertyName == nameof(Order.Balance) || e.PropertyName == nameof(Order.State))
					NotifyChanged(nameof(OrderState));

				if (e.PropertyName == nameof(Order.TimeInForce) || e.PropertyName == nameof(Order.ExpiryDate))
					NotifyChanged(nameof(OrderTif));
			}
		}

		private readonly ConvertibleObservableCollection<Order, OrderItem> _orders;

		/// <summary>
		/// Initializes a new instance of the <see cref="OrderGrid"/>.
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
		/// The maximum number of orders to display. The -1 value means unlimited amount. The default is 100000.
		/// </summary>
		public int MaxCount
		{
			get { return _orders.MaxCount; }
			set { _orders.MaxCount = value; }
		}

		/// <summary>
		/// The list of orders that have been added to the table.
		/// </summary>
		public IListEx<Order> Orders => _orders;

		/// <summary>
		/// To add a description of the registration error to the table.
		/// </summary>
		/// <param name="fail">Error.</param>
		public void AddRegistrationFail(OrderFail fail)
		{
			if (fail == null)
				throw new ArgumentNullException(nameof(fail));

			var item = _orders.TryGet(fail.Order);

			if (item != null)
				item.Comment = fail.Error.Message;
		}

		/// <summary>
		/// The selected order.
		/// </summary>
		public Order SelectedOrder => SelectedOrders.FirstOrDefault();

		/// <summary>
		/// Selected orders.
		/// </summary>
		public IEnumerable<Order> SelectedOrders
		{
			get { return SelectedItems.Cast<OrderItem>().Select(i => i.Order); }
		}

		/// <summary>
		/// The order registration event.
		/// </summary>
		public event Action OrderRegistering;

		/// <summary>
		/// The order re-registration event.
		/// </summary>
		public event Action<Order> OrderReRegistering;

		/// <summary>
		/// The selected orders cancel event.
		/// </summary>
		public event Action<IEnumerable<Order>> OrderCanceling;

		/// <summary>
		/// The method is called when a new order added.
		/// </summary>
		/// <param name="order">Order.</param>
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
				default:
					throw new ArgumentOutOfRangeException(nameof(values), state, LocalizedStrings.Str1597Params.Put(order));
			}
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
			var tif = (TimeInForce?)values[0];
			var expiryDate = (DateTimeOffset?)values[1];

			switch (tif)
			{
				case null:
				case TimeInForce.PutInQueue:
				{
					if (expiryDate == null || expiryDate.Value.IsGtc())
						return "GTC";
					else if (expiryDate.Value.IsToday())
						return "GTD";
					else
						return expiryDate.Value.LocalDateTime.ToString("d");
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