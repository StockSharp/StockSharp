#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: PortfolioMessageAdaptersWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;
	using Row = System.Tuple<string, Messages.IMessageAdapter>;

	/// <summary>
	/// The window for creation and editing of portfolios and adapters comparisons.
	/// </summary>
	public partial class PortfolioMessageAdaptersWindow
	{
		private readonly ObservableCollection<Row> _items = new ObservableCollection<Row>();

		/// <summary>
		/// <see cref="RoutedCommand"/> to remove comparison.
		/// </summary>
		public static readonly RoutedCommand RemoveCommand = new RoutedCommand();

		/// <summary>
		/// <see cref="RoutedCommand"/> to add comparison.
		/// </summary>
		public static readonly RoutedCommand AddCommand = new RoutedCommand();

		private BasketMessageAdapter _adapter;

		/// <summary>
		/// Adapter aggregator.
		/// </summary>
		public BasketMessageAdapter Adapter
		{
			get { return _adapter; }
			set
			{
				if (_adapter == value)
					return;

				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_adapter = value;

				_items.Clear();
				_items.AddRange(_adapter.Portfolios.Select(p => new Row(p.Key, p.Value)));

				AdaptersComboBox.ItemsSource = _adapter.InnerAdapters;
			}
		}

		private Row SelectedItem => (Row)PortfoliosCtrl?.SelectedItem;

		/// <summary>
		/// Initializes a new instance of the <see cref="PortfolioMessageAdaptersWindow"/>.
		/// </summary>
		public PortfolioMessageAdaptersWindow()
		{
			InitializeComponent();

			PortfoliosCtrl.ItemsSource = _items;
			//TODO PortfoliosCtrl.GroupingColumns.Add(PortfoliosCtrl.Columns[0]);
		}

		/// <summary>
		/// Available portfolios.
		/// </summary>
		public ThreadSafeObservableCollection<Portfolio> Portfolios
		{
			get { return PortfoliosComboBox.Portfolios; }
			set { PortfoliosComboBox.Portfolios = value; }
		}

		private void ExecutedRemove(object sender, ExecutedRoutedEventArgs e)
		{
			var item = SelectedItem;

			Adapter.Portfolios.Remove(item.Item1);
			_items.Remove(item);
		}

		private void CanExecuteRemove(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedItem != null;
		}

		private void ExecutedAdd(object sender, ExecutedRoutedEventArgs e)
		{
			var portfolio = PortfoliosComboBox.SelectedPortfolio.Name;
			var adapter = (IMessageAdapter)AdaptersComboBox.SelectedItem;

			if (Adapter.Portfolios.ContainsKey(portfolio))
			{
				new MessageBoxBuilder()
					.Caption(Title)
					.Text(LocalizedStrings.Str1542Params.Put(portfolio))
					.Warning()
					.Show();

				return;
			}

			_items.Add(new Row(portfolio, adapter));
			Adapter.Portfolios[portfolio] = adapter;

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

			var sessionHolder = (IMessageAdapter)value;

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