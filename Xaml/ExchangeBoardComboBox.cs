namespace StockSharp.Xaml
{
	using System.Collections.Generic;
	using System.Windows;
	using System.Windows.Controls;
	
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;
	using StockSharp.Localization;

	/// <summary>
	/// The drop-down list for exchange board selection.
	/// </summary>
	public class ExchangeBoardComboBox : ComboBox
	{
		private static readonly ExchangeBoard _emptyBoard = new ExchangeBoard { Code = LocalizedStrings.Str1521 };
		private readonly ThreadSafeObservableCollection<ExchangeBoard> _boards;

		/// <summary>
		/// Initializes a new instance of the <see cref="ExchangeBoardComboBox"/>.
		/// </summary>
		public ExchangeBoardComboBox()
		{
			IsEditable = true;

			var itemsSource = new ObservableCollectionEx<ExchangeBoard> { _emptyBoard };
			_boards = new ThreadSafeObservableCollection<ExchangeBoard>(itemsSource);

			//_boards.AddRange(ExchangeBoard.EnumerateExchangeBoards().OrderBy(b => b.Code));

			DisplayMemberPath = "Code";

			Boards = _boards;
			ItemsSource = Boards;

			SelectedBoard = _emptyBoard;
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="ExchangeInfoProvider"/>.
		/// </summary>
		public static readonly DependencyProperty ExchangeInfoProviderProperty = DependencyProperty.Register("ExchangeInfoProvider", typeof(IExchangeInfoProvider), typeof(ExchangeBoardComboBox), new PropertyMetadata(null, (o, args) =>
		{
			var cb = (ExchangeBoardComboBox)o;
			cb.UpdateProvider((IExchangeInfoProvider)args.NewValue);
		}));

		private void UpdateProvider(IExchangeInfoProvider provider)
		{
			_boards.Clear();

			if (_exchangeInfoProvider != null)
				_exchangeInfoProvider.BoardAdded -= _boards.Add;

			_exchangeInfoProvider = provider;

			if (_exchangeInfoProvider != null)
			{
				_boards.AddRange(_exchangeInfoProvider.Boards);
				_exchangeInfoProvider.BoardAdded += _boards.Add;
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
		/// All exchange boards.
		/// </summary>
		public IList<ExchangeBoard> Boards { get; }

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="SelectedBoard"/>.
		/// </summary>
		public static readonly DependencyProperty SelectedBoardProperty =
			 DependencyProperty.Register("SelectedBoard", typeof(ExchangeBoard), typeof(ExchangeBoardComboBox),
				new FrameworkPropertyMetadata(null, OnSelectedBoardPropertyChanged));

		private ExchangeBoard _selectedBoard;

		/// <summary>
		/// Selected exchange board.
		/// </summary>
		public ExchangeBoard SelectedBoard
		{
			get
			{
				return _selectedBoard == _emptyBoard ? null : _selectedBoard;
			}
			set
			{
				SetValue(SelectedBoardProperty, value ?? _emptyBoard);
			}
		}

		private static void OnSelectedBoardPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
		{
			var board = (ExchangeBoard)e.NewValue;
			var combo = (ExchangeBoardComboBox)source;

			combo.SelectedItem = board;
			combo._selectedBoard = board;
		}

		/// <summary>
		/// The selected item change event handler.
		/// </summary>
		/// <param name="e">The event parameter.</param>
		protected override void OnSelectionChanged(SelectionChangedEventArgs e)
		{
			base.OnSelectionChanged(e);
			SelectedBoard = (ExchangeBoard)SelectedItem;
		}
	}
}