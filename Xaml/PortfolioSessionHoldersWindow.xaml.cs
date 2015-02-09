namespace StockSharp.Xaml
{
	using System;
	using System.Collections.ObjectModel;
	using System.Globalization;
	using System.Linq;
	using System.Windows.Data;
	using System.Windows.Input;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.Messages;

	using Row = System.Tuple<string, Messages.IMessageSessionHolder>;

	using StockSharp.Localization;

	/// <summary>
	/// Окно создания редактирования сопоставлений портфелей и адаптеров.
	/// </summary>
	public partial class PortfolioSessionHoldersWindow
	{
		private readonly ObservableCollection<Row> _items = new ObservableCollection<Row>();
		private BasketSessionHolder _sessionHolder;

		/// <summary>
		/// <see cref="RoutedCommand"/> на удаление сопоставления.
		/// </summary>
		public static readonly RoutedCommand RemoveCommand = new RoutedCommand();

		/// <summary>
		/// <see cref="RoutedCommand"/> на добавление сопоставления.
		/// </summary>
		public static readonly RoutedCommand AddCommand = new RoutedCommand();

		/// <summary>
		/// Контейнер для сессии.
		/// </summary>
		public BasketSessionHolder SessionHolder
		{
			get { return _sessionHolder; }
			set
			{
				if (_sessionHolder == value)
					return;

				if (value == null)
					throw new ArgumentNullException("value");

				_sessionHolder = value;

				_items.Clear();
				_items.AddRange(_sessionHolder.Portfolios.Select(p => new Row(p.Key, p.Value)));

				AdaptersComboBox.ItemsSource = _sessionHolder.InnerSessions;
			}
		}

		private Row SelectedItem
		{
			get { return PortfoliosCtrl != null ? (Row)PortfoliosCtrl.SelectedItem : null; }
		}

		/// <summary>
		/// Создать <see cref="PortfolioSessionHoldersWindow"/>.
		/// </summary>
		public PortfolioSessionHoldersWindow()
		{
			InitializeComponent();

			PortfoliosCtrl.ItemsSource = _items;
			//TODO PortfoliosCtrl.GroupingColumns.Add(PortfoliosCtrl.Columns[0]);
		}

		private void ExecutedRemove(object sender, ExecutedRoutedEventArgs e)
		{
			var item = SelectedItem;

			SessionHolder.Portfolios.Remove(item.Item1);
			_items.Remove(item);
		}

		private void CanExecuteRemove(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedItem != null;
		}

		private void ExecutedAdd(object sender, ExecutedRoutedEventArgs e)
		{
			var portfolio = PortfoliosComboBox.SelectedPortfolio.Name;
			var sessionHolder = (IMessageSessionHolder)AdaptersComboBox.SelectedItem;

			if (SessionHolder.Portfolios.ContainsKey(portfolio))
			{
				new MessageBoxBuilder()
					.Caption(Title)
					.Text(LocalizedStrings.Str1542Params.Put(portfolio))
					.Warning()
					.Show();

				return;
			}

			_items.Add(new Row(portfolio, sessionHolder));
			SessionHolder.Portfolios[portfolio] = sessionHolder;

			PortfoliosComboBox.SelectedPortfolio = null;
			AdaptersComboBox.SelectedItem = null;
		}

		private void CanExecuteAdd(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = AdaptersComboBox.SelectedItem != null && PortfoliosComboBox.SelectedPortfolio != null;
		}
	}

	class SessionHolderToStringConverter : IValueConverter
	{
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is string)
				return value;

			var sessionHolder = (IMessageSessionHolder)value;

			var title = sessionHolder.GetType().GetDisplayName();
			var descr = sessionHolder.ToString();

			return descr.IsEmpty()
					   ? title
					   : "{0} ({1})".Put(title, descr);
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}