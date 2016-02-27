#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: ExchangeBoardComboBox.cs
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

			DisplayMemberPath = nameof(ExchangeBoard.Code);

			Boards = _boards;
			ItemsSource = Boards;

			SelectedBoard = _emptyBoard;
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="ExchangeInfoProvider"/>.
		/// </summary>
		public static readonly DependencyProperty ExchangeInfoProviderProperty = DependencyProperty.Register(nameof(ExchangeInfoProvider), typeof(IExchangeInfoProvider), typeof(ExchangeBoardComboBox), new PropertyMetadata(null, (o, args) =>
		{
			var cb = (ExchangeBoardComboBox)o;
			cb.UpdateProvider((IExchangeInfoProvider)args.NewValue);
		}));

		private void UpdateProvider(IExchangeInfoProvider provider)
		{
			_boards.Clear();
			_boards.Add(_emptyBoard);

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
			 DependencyProperty.Register(nameof(SelectedBoard), typeof(ExchangeBoard), typeof(ExchangeBoardComboBox),
				new FrameworkPropertyMetadata(null, OnSelectedBoardPropertyChanged));

		private ExchangeBoard _selectedBoard;

		/// <summary>
		/// Selected exchange board.
		/// </summary>
		public ExchangeBoard SelectedBoard
		{
			get { return _selectedBoard == _emptyBoard ? null : _selectedBoard; }
			set { SetValue(SelectedBoardProperty, value); }
		}

		private static void OnSelectedBoardPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
		{
			var board = (ExchangeBoard)e.NewValue;
			var combo = (ExchangeBoardComboBox)source;

			combo._selectedBoard = board;
			combo.SelectedItem = board ?? _emptyBoard;
		}

		/// <summary>
		/// The selected item change event handler.
		/// </summary>
		/// <param name="e">The event parameter.</param>
		protected override void OnSelectionChanged(SelectionChangedEventArgs e)
		{
			base.OnSelectionChanged(e);

			var board = (ExchangeBoard)SelectedItem;
			SelectedBoard = board != _emptyBoard ? board : null;
		}
	}
}