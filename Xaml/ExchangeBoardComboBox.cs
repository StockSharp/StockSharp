namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	
	using Ecng.Collections;
	using Ecng.Configuration;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;

	using StockSharp.Localization;

	/// <summary>
	/// Выпадающий список для выбора биржевой площадки.
	/// </summary>
	public class ExchangeBoardComboBox : ComboBox
	{
		private static readonly ExchangeBoard _emptyBoard = new ExchangeBoard { Code = LocalizedStrings.Str1521 };

		/// <summary>
		/// Создать <see cref="ExchangeBoardComboBox"/>.
		/// </summary>
		public ExchangeBoardComboBox()
		{
			IsEditable = true;

			var provider = ConfigManager.TryGetService<IExchangeInfoProvider>();

			if (provider == null)
			{
				Boards = new ObservableCollectionEx<ExchangeBoard> { _emptyBoard };
				Boards.AddRange(ExchangeBoard.EnumerateExchangeBoards().OrderBy(b => b.Code));

				ConfigManager.ServiceRegistered += ConfigManagerServiceRegistered;
			}
			else
				FillBoards(provider);

			ItemsSource = Boards;
			DisplayMemberPath = "Code";

			SelectedBoard = _emptyBoard;
		}

		private void ConfigManagerServiceRegistered(Type type, object service)
		{
			if (typeof(IExchangeInfoProvider) != type)
				return;

			FillBoards((IExchangeInfoProvider)service);

			GuiDispatcher.GlobalDispatcher.AddAction(() => ItemsSource = Boards);
		}

		private void FillBoards(IExchangeInfoProvider provider)
		{
			var itemsSource = new ObservableCollectionEx<ExchangeBoard> { _emptyBoard };
			var boards = new ThreadSafeObservableCollection<ExchangeBoard>(itemsSource);

			boards.AddRange(provider.Boards);

			provider.BoardAdded += boards.Add;
			
			Boards = boards;
		}

		/// <summary>
		/// Все биржевые площадки.
		/// </summary>
		public IList<ExchangeBoard> Boards { get; private set; }

		/// <summary>
		/// <see cref="DependencyProperty"/> для <see cref="SelectedBoard"/>.
		/// </summary>
		public static readonly DependencyProperty SelectedBoardProperty =
			 DependencyProperty.Register("SelectedBoard", typeof(ExchangeBoard), typeof(ExchangeBoardComboBox),
				new FrameworkPropertyMetadata(null, OnSelectedBoardPropertyChanged));

		private ExchangeBoard _selectedBoard;

		/// <summary>
		/// Выбранная биржевая площадка.
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
			var editor = (ExchangeBoardComboBox)source;

			editor.SelectedItem = board;
			editor._selectedBoard = board;
		}

		/// <summary>
		/// Обработчик события смены выбранного элемента.
		/// </summary>
		/// <param name="e">Параметр события.</param>
		protected override void OnSelectionChanged(SelectionChangedEventArgs e)
		{
			base.OnSelectionChanged(e);
			SelectedBoard = (ExchangeBoard)SelectedItem;
		}
	}
}