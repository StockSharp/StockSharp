namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Xaml;

	using StockSharp.Messages;

	/// <summary>
	/// Таблица, отображающая (<see cref="ExecutionMessage"/>.
	/// </summary>
	public partial class ExecutionGrid
	{
		private readonly ThreadSafeObservableCollection<ExecutionMessage> _messages;

		/// <summary>
		/// Создать <see cref="ExecutionGrid"/>.
		/// </summary>
		public ExecutionGrid()
		{
			InitializeComponent();

			var itemsSource = new ObservableCollectionEx<ExecutionMessage>();
			ItemsSource = itemsSource;

			_messages = new ThreadSafeObservableCollection<ExecutionMessage>(itemsSource) { MaxCount = 1000000 };
		}

		/// <summary>
		/// Максимальное число строк для показа. Значение -1 означает бесконечное количество.
		/// По-умолчанию равно 1000000.
		/// </summary>
		public int MaxCount
		{
			get { return _messages.MaxCount; }
			set { _messages.MaxCount = value; }
		}

		/// <summary>
		/// Список сообщений, добавленных в таблицу.
		/// </summary>
		public IListEx<ExecutionMessage> Messages
		{
			get { return _messages; }
		}

		/// <summary>
		/// Выбранное сообщение.
		/// </summary>
		public ExecutionMessage SelectedMessage
		{
			get { return SelectedMessages.FirstOrDefault(); }
		}

		/// <summary>
		/// Выбранные сообщения.
		/// </summary>
		public IEnumerable<ExecutionMessage> SelectedMessages
		{
			get { return SelectedItems.Cast<ExecutionMessage>(); }
		}

		/// <summary>
		/// Скрыть колонки, не показывающие данные для переданного типа.
		/// </summary>
		/// <param name="type">Тип информации в <see cref="ExecutionMessage"/>.</param>
		public void HideColumns(ExecutionTypes type)
		{
			switch (type)
			{
				case ExecutionTypes.Tick:
				{
					OrderIdColumn.Visibility =
					OrderPriceColumn.Visibility =
					OrderTypeColumn.Visibility =
					OrderSideColumn.Visibility =
					OrderStateColumn.Visibility =
					TransactionColumn.Visibility =
					PortfolioColumn.Visibility =
					BalanceColumn.Visibility =
						Visibility.Collapsed;
					break;
				}
				case ExecutionTypes.Order:
				{
					TradeIdColumn.Visibility = TradePriceColumn.Visibility = Visibility.Collapsed;
					break;
				}
				case ExecutionTypes.Trade:
					break;
				case ExecutionTypes.OrderLog:
					break;
				default:
					throw new ArgumentOutOfRangeException("type");
			}
		}
	}
}