namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Linq;
	using System.Windows.Controls;
	using System.Windows.Input;
	using System.Globalization;
	using System.Windows;
	using System.Windows.Data;
	using System.Text.RegularExpressions;
	using System.Linq.Expressions;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Configuration;
	using Ecng.Collections;
	using Ecng.Serialization;
	using Ecng.Xaml;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	using MoreLinq;

	using StockSharp.Localization;

	/// <summary>
	/// Редактор биржевых площадок.
	/// </summary>
	public partial class ExchangeBoardEditor : IPersistable
	{
		/// <summary>
		/// Сообщение об ошибке сохранения данных.
		/// </summary>
		public static readonly DependencyProperty SaveErrorMessageProperty = DependencyProperty.Register("SaveErrorMessage", typeof(string), typeof(ExchangeBoardEditor), new PropertyMetadata(default(string)));

		private IExchangeInfoProvider Provider { get; set; }

		/// <summary>
		/// Список площадок.
		/// </summary>
		public ObservableCollection<ExchangeBoard> Boards { get; private set; }

		private ExchangeBoardEditorViewModel ViewModel
		{
			get { return (ExchangeBoardEditorViewModel)DataContext; }
		}

		internal string SaveErrorMessage
		{
			get { return (string) GetValue(SaveErrorMessageProperty); }
			private set { SetValue(SaveErrorMessageProperty, value); }
		}

		internal Exchange SelectedExchange
		{
			get { return _exchangeEditor.GetSelectedExchange(); }
		}

		/// <summary>
		/// Код площадки, которая в данный момент редактируется.
		/// </summary>
		public string SelectedBoardCode { get { return ViewModel.BoardCode; }}

		private static readonly Regex _checkCodeRegex = new Regex("^[a-z0-9]{1,15}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		private DateTime _autoResetMessageTimestamp;
		private string _errorMessage, _autoResetErrorMessage;
		private bool _updatingUI;

		/// <summary>
		/// Создать <see cref="ExchangeBoardEditor"/>.
		/// </summary>
		public ExchangeBoardEditor()
		{
			Provider = ConfigManager.GetService<IExchangeInfoProvider>();

			Boards = new ObservableCollection<ExchangeBoard>(Provider.Boards);

			InitializeComponent();
			DataContext = new ExchangeBoardEditorViewModel(this, Provider);
			ViewModel.DataChanged += VmOnDataChanged;

			Provider.BoardAdded += ProviderOnBoardAdded;

			GuiDispatcher.GlobalDispatcher.AddPeriodicalAction(OnTimer);

			SetBoardCode(ExchangeBoard.Nasdaq.Code);
		}

		/// <summary>
		/// Установить редактируемую площадку.
		/// </summary>
		/// <param name="boardCode">Код редактируемой площадки. Площадка для редактирования загружается по коду из EntityRegistry.</param>
		public void SetBoardCode(string boardCode)
		{
			_updatingUI = true;

			try
			{
				if (boardCode.IsEmpty())
				{
					ResetBoardEditor();
					_exchangeEditor.SetExchange(null);
					return;
				}

				ViewModel.SetBoard(boardCode);
				CbBoardCode.SelectedItem = Boards.FirstOrDefault(b => b.Code == ViewModel.BoardCode);
				_exchangeEditor.SetExchange(ViewModel.Board == null ? null : ViewModel.Board.Exchange.Name);
			}
			finally
			{
				_updatingUI = false;
			}
		}

		private void AutoCompleteSelector_OnKeyDown(object sender, KeyEventArgs e)
		{
			((ComboBox)sender).IsDropDownOpen = true;
		}

		private void BoardSelector_OnSelectionChanged(object sender, EventArgs e)
		{
			var cb = (ComboBox)sender;
			if (cb.IsDropDownOpen || ViewModel == null || _updatingUI)
				return;

			if (cb.SelectedItem != null)
			{
				var curText = cb.SelectedValue as string;
				if (!curText.IsEmpty())
					SetBoardCode(curText);
			}
			else
			{
				var curText = cb.Text.Trim();
				if (curText.IsEmpty())
					return;

				_exchangeEditor.SetExchange(null);
				SetBoardCode(curText);
			}
		}

		private void ButtonClear_Click(object sender, RoutedEventArgs e)
		{
			ResetBoardEditor();
			_exchangeEditor.SetExchange(null);
		}

		private void ResetBoardEditor()
		{
			_updatingUI = true;

			try
			{
				ViewModel.SetBoard(null);
				SetSaveError(null);
				CbBoardCode.Text = string.Empty;
				_exchangeEditor.SetExchange(null);
			}
			finally
			{
				_updatingUI = false;
			}
		}

		private void CodeBox_Loaded(object sender, RoutedEventArgs e)
		{
			var cb = (ComboBox)sender;
			var tb = cb.Template.FindName("PART_EditableTextBox", cb) as TextBox;
			if(tb == null)
				return;

			DataObject.AddPastingHandler(tb, CheckPasteFormat);

			tb.CharacterCasing = CharacterCasing.Upper;
			tb.PreviewTextInput += (o, args) =>
			{
				var newText = tb.Text + args.Text;
				if(!_checkCodeRegex.IsMatch(newText))
					args.Handled = true;
			};
		}

		private static void CheckPasteFormat(object sender, DataObjectPastingEventArgs args)
		{
			var tb = (TextBox)sender;

			var str = args.DataObject.GetData(typeof(string)) as string;
			var newStr = tb.Text + str;

			if (!_checkCodeRegex.IsMatch(newStr))
				args.CancelCommand();
		}

		private void ProviderOnBoardAdded(ExchangeBoard board)
		{
			Dispatcher.GuiAsync(() =>
			{
				//using (Dispatcher.DisableProcessing())
				try
				{
					_updatingUI = true;

					//var selectedVal = CbBoardCode.SelectedValue as string ?? CbBoardCode.Text.Trim();
					//Boards.Clear();

					Boards.Add(board);

					CbBoardCode.SelectedItem = board;
					//if (!selectedVal.IsEmpty())
					SetBoardCode(board.Code);
				}
				finally
				{
					_updatingUI = false;
				}
			});
		}

		private void ExchangeEditor_OnSelectedExchangeChanged()
		{
			if (!_updatingUI)
				ViewModel.SaveBoard(_exchangeEditor.GetSelectedExchange());
		}

		private void VmOnDataChanged()
		{
			if (!_updatingUI)
				ViewModel.SaveBoard(_exchangeEditor.GetSelectedExchange());
		}

		private void OnTimer()
		{
			if (_autoResetErrorMessage.IsEmpty() || DateTime.UtcNow - _autoResetMessageTimestamp <= TimeSpan.FromSeconds(5))
				return;

			_autoResetErrorMessage = null;
			SaveErrorMessage = _errorMessage;
		}

		internal void SetSaveError(string message, bool autoReset = false)
		{
			if (message.IsEmpty())
			{
				SaveErrorMessage = _errorMessage = _autoResetErrorMessage = null;
				return;
			}

			if (autoReset)
			{
				if(_autoResetErrorMessage.IsEmpty())
					_errorMessage = SaveErrorMessage;

				SaveErrorMessage = _autoResetErrorMessage = message;
				_autoResetMessageTimestamp = DateTime.UtcNow;
			} 
			else
			{
				_errorMessage = message;

				if (_autoResetErrorMessage.IsEmpty())
					SaveErrorMessage = _errorMessage;
			}
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public void Save(SettingsStorage storage)
		{
			var boardCode = SelectedBoardCode;

			if (!boardCode.IsEmpty())
				storage.SetValue("SelectedBoard", boardCode);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public void Load(SettingsStorage storage)
		{
			SetBoardCode(storage.GetValue("SelectedBoard", ExchangeBoard.Nasdaq.Code));
		}
	}

	class ExchangeBoardEditorViewModel : ViewModelBase
	{
		#region commands definition

		private ICommand _commandAddNewBoard;
		private ICommand _commandAddPeriod, _commandDelPeriod;
		private ICommand _commandAddTimes, _commandDelTimes;
		private ICommand _commandAddSpecialWorkingDay, _commandDelSpecialWorkingDay;
		private ICommand _commandAddSpecialHoliday, _commandDelSpecialHoliday;

		public ICommand CommandAddNewBoard
		{
			get { return _commandAddNewBoard ?? (_commandAddNewBoard = new DelegateCommand(o => CmdAddNewBoard(_editor.SelectedExchange), o => true)); }
		}

		public ICommand CommandAddPeriod
		{
			get { return _commandAddPeriod ?? (_commandAddPeriod = new DelegateCommand(o => CmdAddPeriod(), o => true)); }
		}

		public ICommand CommandDelPeriod
		{
			get { return _commandDelPeriod ?? (_commandDelPeriod = new DelegateCommand(o => CmdDelPeriod(), o => true)); }
		}

		public ICommand CommandAddTimes
		{
			get { return _commandAddTimes ?? (_commandAddTimes = new DelegateCommand(o => CmdAddTimes(), o => true)); }
		}

		public ICommand CommandDelTimes
		{
			get { return _commandDelTimes ?? (_commandDelTimes = new DelegateCommand(o => CmdDelTimes(), o => true)); }
		}

		public ICommand CommandAddSpecialWorkingDay
		{
			get { return _commandAddSpecialWorkingDay ?? (_commandAddSpecialWorkingDay = new DelegateCommand(o => CmdAddSpecialWorkingDay(), o => true)); }
		}

		public ICommand CommandDelSpecialWorkingDay
		{
			get { return _commandDelSpecialWorkingDay ?? (_commandDelSpecialWorkingDay = new DelegateCommand(o => CmdDelSpecialWorkingDays(), o => true)); }
		}

		public ICommand CommandAddSpecialHoliday
		{
			get { return _commandAddSpecialHoliday ?? (_commandAddSpecialHoliday = new DelegateCommand(o => CmdAddSpecialHoliday(), o => true)); }
		}

		public ICommand CommandDelSpecialHoliday
		{
			get { return _commandDelSpecialHoliday ?? (_commandDelSpecialHoliday = new DelegateCommand(o => CmdDelSpecialHolidays(), o => true)); }
		}

		#endregion

		private ExchangeBoard _board;
		private bool _isNewBoard;
		private bool _isSupportAtomicReRegister, _isSupportMarketOrders;
		private DateTime _expiryTime, _newPeriodTill, _newTimeFrom, _newTimeTill, _newSpecialWorkingDay, _newSpecialHoliday;
		private readonly ObservableCollection<WorkingTimePeriodViewModel> _periods = new ObservableCollection<WorkingTimePeriodViewModel>();
		private WorkingTimePeriodViewModel _selectedPeriod;
		private readonly ObservableCollection<DateTimeViewModel> _specialWorkingDays = new ObservableCollection<DateTimeViewModel>();
		private readonly ObservableCollection<DateTimeViewModel> _specialHolidays = new ObservableCollection<DateTimeViewModel>();

		private readonly ExchangeBoardEditor _editor;

		private readonly IExchangeInfoProvider _provider;

		private bool _updatingModel;

		private IExchangeInfoProvider Provider
		{
			get { return _provider; }
		}

		private IEnumerable<ExchangeBoard> Boards
		{
			get { return _editor.Boards; }
		}

		public event Action DataChanged;

		public ExchangeBoardEditorViewModel(ExchangeBoardEditor editor, IExchangeInfoProvider provider)
		{
			if (editor == null)
				throw new ArgumentNullException("editor");

			if (provider == null)
				throw new ArgumentNullException("provider");

			_editor = editor;
			_provider = provider;
			IsNewBoard = true;

			NewPeriodTill = new DateTime(DateTime.Now.Year + 1, 1, 1) - TimeSpan.FromDays(1);
			NewTimeFrom = NewTimeTill = DateTime.MinValue;
			NewSpecialWorkingDay = NewSpecialHoliday = new DateTime(DateTime.Now.Year, 1, 1);
		}

		public void SetBoard(string boardCode)
		{
			_updatingModel = true;

			try
			{
				var newBoard = Boards.FirstOrDefault(b => b.Code == boardCode);
				if (newBoard != null && ReferenceEquals(newBoard, Board))
					return;

				Reset();

				if (boardCode.IsEmpty())
					return;

				boardCode = boardCode.ToUpperInvariant();
				BoardCode = boardCode;

				Board = newBoard;
				IsNewBoard = Board == null;
				if (!IsNewBoard)
					LoadFromBoard();

				if (IsNewBoard)
					_editor.SetSaveError(LocalizedStrings.Str1425);
			}
			finally
			{
				_updatingModel = false;
			}
		}

		public void SaveBoard(Exchange exchange)
		{
			if (_updatingModel || IsNewBoard || Board == null || !IsBoardValid() || exchange == null)
				return;

			Board = CreateBoardFromData();
			Board.Exchange = exchange;
			Board.Code = BoardCode;

			Provider.Save(Board);
		}

		private void LoadFromBoard()
		{
			IsSupportAtomicReRegister = Board.IsSupportAtomicReRegister;
			IsSupportMarketOrders = Board.IsSupportMarketOrders;
			ExpiryTime = DateTime.MinValue + Board.ExpiryTime;

			_periods.Clear();
			_specialWorkingDays.Clear();
			_specialHolidays.Clear();

			Board.WorkingTime.Periods.ForEach(p => _periods.Add(new WorkingTimePeriodViewModel(p)));
			Board.WorkingTime.SpecialWorkingDays.ForEach(d => _specialWorkingDays.Add(new DateTimeViewModel(d)));
			Board.WorkingTime.SpecialHolidays.ForEach(d => _specialHolidays.Add(new DateTimeViewModel(d)));
		}

		private void Reset()
		{
			Board = null;
			BoardCode = null;
			IsNewBoard = true;

			IsSupportAtomicReRegister = false;
			IsSupportMarketOrders = false;
			ExpiryTime = DateTime.MinValue;

			_periods.Clear();
			_specialWorkingDays.Clear();
			_specialHolidays.Clear();
		}

		#region board parameters

		public ExchangeBoard Board
		{
			get { return _board; }
			private set { SetField(ref _board, value, () => Board, vmDataChanged: false); }
		}

		public string BoardCode { get; private set; }

		public bool IsNewBoard
		{
			get { return _isNewBoard; }
			set { SetField(ref _isNewBoard, value, () => IsNewBoard, vmDataChanged: false); }
		}

		public bool IsSupportAtomicReRegister
		{
			get { return _isSupportAtomicReRegister; }
			set { SetField(ref _isSupportAtomicReRegister, value, () => IsSupportAtomicReRegister); }
		}

		public bool IsSupportMarketOrders
		{
			get { return _isSupportMarketOrders; }
			set { SetField(ref _isSupportMarketOrders, value, () => IsSupportMarketOrders); }
		}

		public DateTime ExpiryTime
		{
			get { return _expiryTime; }
			set { SetField(ref _expiryTime, value, () => ExpiryTime); }
		}

		public IEnumerable<WorkingTimePeriodViewModel> Periods
		{
			get { return _periods; }
		}

		public IEnumerable<DateTimeViewModel> SpecialWorkingDays
		{
			get { return _specialWorkingDays; }
		}

		public IEnumerable<DateTimeViewModel> SpecialHolidays
		{
			get { return _specialHolidays; }
		}

		public WorkingTimePeriodViewModel SelectedPeriod
		{
			get { return _selectedPeriod; }
			set { SetField(ref _selectedPeriod, value, () => SelectedPeriod, vmDataChanged: false); }
		}

		public DateTime NewPeriodTill
		{
			get { return _newPeriodTill; }
			set { SetField(ref _newPeriodTill, value, () => NewPeriodTill, vmDataChanged: false); }
		}

		public DateTime NewTimeFrom
		{
			get { return _newTimeFrom; }
			set { SetField(ref _newTimeFrom, value, () => NewTimeFrom, vmDataChanged: false); }
		}

		public DateTime NewTimeTill
		{
			get { return _newTimeTill; }
			set { SetField(ref _newTimeTill, value, () => NewTimeTill, vmDataChanged: false); }
		}

		public DateTime NewSpecialWorkingDay
		{
			get { return _newSpecialWorkingDay; }
			set { SetField(ref _newSpecialWorkingDay, value, () => NewSpecialWorkingDay, vmDataChanged: false); }
		}

		public DateTime NewSpecialHoliday
		{
			get { return _newSpecialHoliday; }
			set { SetField(ref _newSpecialHoliday, value, () => NewSpecialHoliday, vmDataChanged: false); }
		}

		#endregion

		#region commands implementation

		private void CmdAddNewBoard(Exchange exchange)
		{
			if (exchange == null)
			{
				_editor.SetSaveError(LocalizedStrings.Str1426);
				return;
			}

			if (!IsNewBoard || !IsBoardValid())
				return;

			Board = CreateBoardFromData();
			Board.Exchange = exchange;
			IsNewBoard = false;
			_editor.SetSaveError(null);
			Provider.Save(Board);
		}

		private void CmdAddPeriod()
		{
			var date = NewPeriodTill.Date;
			if (Periods.FirstOrDefault(p => p.Till == date) != null)
			{
				_editor.SetSaveError(LocalizedStrings.Str1427, true);
				return;
			}

			var index = _periods.IndexOf(p => p.Till > date);
			if (index < 0)
				_periods.Add(new WorkingTimePeriodViewModel(date));
			else
				_periods.Insert(index, new WorkingTimePeriodViewModel(date));

			OnDataChanged();
		}

		private void CmdDelPeriod()
		{
			var period = SelectedPeriod;
			if (period == null)
				return;

			_periods.RemoveWhere(p => p.Till == period.Till);
			OnDataChanged();
		}

		private void CmdAddTimes()
		{
			if (SelectedPeriod == null)
				return;

			var from = NewTimeFrom.TimeOfDay;
			var till = NewTimeTill.TimeOfDay;

			if (!(till > from))
			{
				_editor.SetSaveError(LocalizedStrings.Str1428, true);
				return;
			}

			var index = SelectedPeriod.WorkTimes.IndexOf(r => r.Min > till);
			if (index == 0)
				SelectedPeriod.WorkTimes.Insert(0, new Range<TimeSpan>(from, till));
			else if (index > 0 && SelectedPeriod.WorkTimes[index - 1].Max < from)
				SelectedPeriod.WorkTimes.Insert(index, new Range<TimeSpan>(from, till));
			else if (index < 0 && (!SelectedPeriod.WorkTimes.Any() || SelectedPeriod.WorkTimes.Last().Max < from))
				SelectedPeriod.WorkTimes.Add(new Range<TimeSpan>(from, till));
			else
				_editor.SetSaveError(LocalizedStrings.Str1429, true);

			OnDataChanged();
		}

		private void CmdDelTimes()
		{
			if (SelectedPeriod == null || SelectedPeriod.SelectedWorkTime == null)
				return;

			SelectedPeriod.WorkTimes.Remove(SelectedPeriod.SelectedWorkTime);

			OnDataChanged();
		}

		private void CmdAddSpecialWorkingDay()
		{
			var date = NewSpecialWorkingDay.Date;
			if (_specialHolidays.FirstOrDefault(m => m.DateTime == date) != null)
			{
				_editor.SetSaveError(LocalizedStrings.Str1430, true);
				return;
			}

			AddToSelectableList(_specialWorkingDays, date);
		}

		private void CmdDelSpecialWorkingDays()
		{
			DeleteFromSelectableList(_specialWorkingDays);
		}

		private void CmdAddSpecialHoliday()
		{
			var date = NewSpecialHoliday.Date;
			if (_specialWorkingDays.FirstOrDefault(m => m.DateTime == date) != null)
			{
				_editor.SetSaveError(LocalizedStrings.Str1431, true);
				return;
			}

			AddToSelectableList(_specialHolidays, date);
		}

		private void CmdDelSpecialHolidays()
		{
			DeleteFromSelectableList(_specialHolidays);
		}

		private void AddToSelectableList(IList<DateTimeViewModel> coll, DateTime date)
		{
			if (coll.FirstOrDefault(m => m.DateTime == date) != null)
			{
				_editor.SetSaveError(LocalizedStrings.Str1432, true);
				return;
			}

			var index = coll.IndexOf(dt => dt.DateTime > date);
			if (index < 0)
				coll.Add(new DateTimeViewModel(date));
			else
				coll.Insert(index, new DateTimeViewModel(date));

			OnDataChanged();
		}

		private void DeleteFromSelectableList(IList<DateTimeViewModel> coll)
		{
			var indexFirst = coll.IndexOf(m => m.IsSelected);
			if(indexFirst < 0)
				return;

			coll.RemoveWhere(m => m.IsSelected);

			if (coll.Any())
				coll[indexFirst == coll.Count ? indexFirst - 1 : indexFirst].IsSelected = true;

			OnDataChanged();
		}

		#endregion

		private bool IsBoardValid()
		{
			if (BoardCode.IsEmpty())
			{
				_editor.SetSaveError(LocalizedStrings.Str1433);
				return false;
			}

			if (SpecialHolidays.Intersect(SpecialWorkingDays).Any())
			{
				_editor.SetSaveError(LocalizedStrings.Str1434);
				return false;
			}

			return true;
		}

		private ExchangeBoard CreateBoardFromData()
		{
			return new ExchangeBoard
			{
				Code = BoardCode,
				ExpiryTime = ExpiryTime.TimeOfDay,
				IsSupportAtomicReRegister = IsSupportAtomicReRegister,
				IsSupportMarketOrders = IsSupportMarketOrders,
				WorkingTime = new WorkingTime
				{
					Periods = Periods.Select(p => new WorkingTimePeriod
					{
						Till = p.Till,
						Times = p.WorkTimes.ToArray()
					}).ToArray(),
					SpecialWorkingDays = SpecialWorkingDays.Select(m => m.DateTime).ToArray(),
					SpecialHolidays = SpecialHolidays.Select(m => m.DateTime).ToArray()
				},
			};
		}

		private void SetField<T>(ref T field, T value, Expression<Func<T>> selectorExpression, bool vmDataChanged = true)
		{
			var changed = base.SetField(ref field, value, selectorExpression);

			if(vmDataChanged && changed)
				OnDataChanged();
		}

		private void OnDataChanged()
		{
			if (!_updatingModel)
				DataChanged.SafeInvoke();
		}

		public class WorkingTimePeriodViewModel : ViewModelBase
		{
			private readonly DateTime _till;
			private readonly ObservableCollection<Range<TimeSpan>> _workTimes = new ObservableCollection<Range<TimeSpan>>();

			private Range<TimeSpan> _selectedWorkTime;

			public WorkingTimePeriodViewModel(DateTime till)
			{
				_till = till;
			}

			public WorkingTimePeriodViewModel(WorkingTimePeriod period)
			{
				_till = period.Till;
				period.Times.ForEach(r => _workTimes.Add(r));
			}

			public DateTime Till
			{
				get { return _till; }
			}

			public ObservableCollection<Range<TimeSpan>> WorkTimes
			{
				get { return _workTimes; }
			}

			public Range<TimeSpan> SelectedWorkTime
			{
				get { return _selectedWorkTime; }
				set { SetField(ref _selectedWorkTime, value, () => SelectedWorkTime); }
			}
		}

		public class DateTimeViewModel : ViewModelBase
		{
			private readonly DateTime _dateTime;
			private bool _isSelected;

			public DateTimeViewModel(DateTime dateTime)
			{
				_dateTime = dateTime;
			}

			public DateTime DateTime
			{
				get { return _dateTime; }
			}

			public bool IsSelected
			{
				get { return _isSelected; }
				set { SetField(ref _isSelected, value, () => IsSelected); }
			}
		}
	}

	class SelectedPeriodBoolConverter : IValueConverter
	{
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value != null;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
