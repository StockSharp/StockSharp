#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: ExchangeComboBox.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System.Collections.Generic;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Xaml;

	using StockSharp.BusinessEntities;
	using StockSharp.Localization;

	/// <summary>
	/// The drop-down list for exchange selection.
	/// </summary>
	public class ExchangeComboBox : ComboBox
	{
		private static readonly Exchange _emptyExchange = new Exchange { Name = LocalizedStrings.Str1521 };
		private readonly ThreadSafeObservableCollection<Exchange> _exchanges;

		/// <summary>
		/// Initializes a new instance of the <see cref="ExchangeComboBox"/>.
		/// </summary>
		public ExchangeComboBox()
		{
			IsEditable = true;

			var itemsSource = new ObservableCollectionEx<Exchange> { _emptyExchange };
			_exchanges = new ThreadSafeObservableCollection<Exchange>(itemsSource);

			//_exchanges.AddRange(Exchange.EnumerateExchanges().OrderBy(b => b.Name));

			DisplayMemberPath = nameof(Exchange.Name);

			Exchanges = _exchanges;
			ItemsSource = Exchanges;

			SelectedExchange = _emptyExchange;
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="ExchangeInfoProvider"/>.
		/// </summary>
		public static readonly DependencyProperty ExchangeInfoProviderProperty = DependencyProperty.Register(nameof(ExchangeInfoProvider), typeof(IExchangeInfoProvider), typeof(ExchangeComboBox), new PropertyMetadata(null, (o, args) =>
		{
			var cb = (ExchangeComboBox)o;
			cb.UpdateProvider((IExchangeInfoProvider)args.NewValue);
		}));

		private void UpdateProvider(IExchangeInfoProvider provider)
		{
			_exchanges.Clear();

			if (_exchangeInfoProvider != null)
				_exchangeInfoProvider.ExchangeAdded -= _exchanges.Add;

			_exchangeInfoProvider = provider;

			if (_exchangeInfoProvider != null)
			{
				_exchanges.AddRange(_exchangeInfoProvider.Exchanges);
				_exchangeInfoProvider.ExchangeAdded += _exchanges.Add;
			}
		}

		private IExchangeInfoProvider _exchangeInfoProvider;

		/// <summary>
		/// The exchange info provider.
		/// </summary>
		public IExchangeInfoProvider ExchangeInfoProvider
		{
			get { return _exchangeInfoProvider; }
			set { SetValue(ExchangeInfoProviderProperty, value); }
		}

		/// <summary>
		/// All exchanges.
		/// </summary>
		public IList<Exchange> Exchanges { get; }

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="SelectedExchange"/>.
		/// </summary>
		public static readonly DependencyProperty SelectedExchangeProperty =
			DependencyProperty.Register(nameof(SelectedExchange), typeof(Exchange), typeof(ExchangeComboBox),
				new FrameworkPropertyMetadata(null, OnSelectedExchangePropertyChanged));

		private Exchange _selectedExchange;

		/// <summary>
		/// Selected exchange.
		/// </summary>
		public Exchange SelectedExchange
		{
			get
			{
				return _selectedExchange == _emptyExchange ? null : _selectedExchange;
			}
			set
			{
				SetValue(SelectedExchangeProperty, value ?? _emptyExchange);
			}
		}

		private static void OnSelectedExchangePropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
		{
			var exchange = (Exchange)e.NewValue;
			var combo = (ExchangeComboBox)source;

			combo.SelectedItem = exchange;
			combo._selectedExchange = exchange;
		}

		/// <summary>
		/// The selected item change event handler.
		/// </summary>
		/// <param name="e">The event parameter.</param>
		protected override void OnSelectionChanged(SelectionChangedEventArgs e)
		{
			base.OnSelectionChanged(e);
			SelectedExchange = (Exchange)SelectedItem;
		}
	}
}