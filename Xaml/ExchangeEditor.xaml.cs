namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Linq;
	using System.Windows.Controls;
	using System.Windows.Input;
	using System.Windows;
	using System.Text.RegularExpressions;
	using System.Linq.Expressions;

	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;

	using StockSharp.Localization;

	/// <summary>
	/// Редактор биржевых площадок.
	/// </summary>
	public partial class ExchangeEditor
	{
		private IExchangeInfoProvider Provider { get; set; }

		/// <summary>
		/// Список бирж.
		/// </summary>
		public ObservableCollection<Exchange> Exchanges { get; private set; }

		/// <summary>
		/// Список временных зон.
		/// </summary>
		public IEnumerable<TimeZoneInfo> TimeZones { get; private set; }

		private ExchangeEditorViewModel ViewModel
		{
			get { return (ExchangeEditorViewModel)DataContext; }
		}

		private static readonly Regex _checkCodeRegex = new Regex("^[a-z0-9]{1,15}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		private string _selectedExchangeName;

		/// <summary>
		/// Имя выбранной биржи.
		/// </summary>
		public string SelectedExchangeName
		{
			get
			{
				return _selectedExchangeName;
			}
			private set
			{
				if (_selectedExchangeName == value)
					return;

				_selectedExchangeName = value;
				SelectedExchangeChanged.SafeInvoke();
			}
		}

		/// <summary>
		/// Событие смены выбранной биржи.
		/// </summary>
		public event Action SelectedExchangeChanged;

		/// <summary>
		/// Создать <see cref="ExchangeEditor"/>.
		/// </summary>
		public ExchangeEditor()
		{
			Provider = ConfigManager.GetService<IExchangeInfoProvider>();

			Exchanges = new ObservableCollection<Exchange>(Provider.Exchanges);

			TimeZones = TimeZoneInfo.GetSystemTimeZones()
				.Concat(new[] { Exchange.Moex.TimeZoneInfo, Exchange.Test.TimeZoneInfo, Exchange.Ux.TimeZoneInfo })
				.Distinct()
				.ToList();

			InitializeComponent();

			var vm = new ExchangeEditorViewModel(this, Provider);
			vm.DataChanged += VmOnDataChanged;
			DataContext = vm;

			Provider.ExchangeAdded += ProviderOnExchangeAdded;
		}

		private void ProviderOnExchangeAdded(Exchange exchange)
		{
			Dispatcher.GuiAsync(() =>
			{
				//using (Dispatcher.DisableProcessing())
				//{
					//var selectedVal = CbExchangeName.SelectedValue as string ?? CbExchangeName.Text.Trim();
					//Exchanges.Clear();

					Exchanges.Add(exchange);

					CbExchangeName.SelectedItem = exchange;
					//if(!SelectedExchangeName.IsEmpty())
					SetExchange(exchange.Name);
				//}
			});
		}

		/// <summary>
		/// Установить редактируемую биржу.
		/// </summary>
		/// <param name="exchangeName">Код редактируемой биржи. Биржа для редактирования загружается из <see cref="IExchangeInfoProvider"/>.</param>
		public void SetExchange(string exchangeName)
		{
			if (exchangeName.IsEmpty())
			{
				ResetEditor();
				return;
			}

			ViewModel.SetExchange(exchangeName);
			SelectedExchangeName = ViewModel.ExchangeName;
			CbExchangeName.SelectedItem = Exchanges.FirstOrDefault(e => e.Name == SelectedExchangeName);
		}

		internal Exchange GetSelectedExchange()
		{
			return ViewModel.Exchange;
		}

		private void AutoCompleteSelector_OnKeyDown(object sender, KeyEventArgs e)
		{
			((ComboBox)sender).IsDropDownOpen = true;
		}

		private void ExchangeSelector_OnSelectionChanged(object sender, EventArgs e)
		{
			var cb = (ComboBox)sender;
			if (cb.IsDropDownOpen)
				return;

			var curText = cb.SelectedValue as string ?? cb.Text.Trim();

			if (curText.IsEmpty())
				return;

			ViewModel.SetExchange(curText);
			SelectedExchangeName = ViewModel.ExchangeName;
		}

		private void ResetEditor()
		{
			ViewModel.SetExchange(null);
			SelectedExchangeName = null;
			CbExchangeName.SelectedItem = null;
			CbExchangeName.Text = string.Empty;
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

		private void VmOnDataChanged()
		{
			ViewModel.SaveExchange();
		}
	}

	class ExchangeEditorViewModel : ViewModelBase
	{
		#region commands definition

		private ICommand _commandAddNewExchange;

		public ICommand CommandAddNewExchange
		{
			get { return _commandAddNewExchange ?? (_commandAddNewExchange = new DelegateCommand(o => CmdAddNewExchange(), o => true)); }
		}

		#endregion

		private Exchange _exchange;
		private bool _isNew;
		private string _exchangeRusName, _exchangeEngName, _saveErrorMessage;
		private CountryCodes? _countryCode;
		private TimeZoneInfo _exchangeTimeZone;

		private readonly ExchangeEditor _editor;
		private readonly IExchangeInfoProvider _provider;

		private bool _updatingModel;

		public event Action DataChanged;

		private IExchangeInfoProvider Provider
		{
			get { return _provider; }
		}

		private IEnumerable<Exchange> Exchanges
		{
			get { return _editor.Exchanges; }
		}

		public ExchangeEditorViewModel(ExchangeEditor editor, IExchangeInfoProvider provider)
		{
			if (editor == null)
				throw new ArgumentNullException("editor");

			if (provider == null)
				throw new ArgumentNullException("provider");

			_editor = editor;
			_provider = provider;
			_isNew = true;
		}

		public void SetExchange(string exchangeName)
		{
			if (exchangeName.IsEmpty())
			{
				Reset();
			}
			else
			{
				exchangeName = exchangeName.ToUpperInvariant();
				ExchangeName = exchangeName;

				Exchange = Exchanges.FirstOrDefault(e => e.Name == exchangeName);
				if (Exchange != null)
					LoadFromExchange();
				else
				{
					Reset();
					ExchangeName = exchangeName;
				}
			}

			IsNew = Exchange == null;
	
			SaveErrorMessage = IsNew ? LocalizedStrings.Str1460 : null;
		}

		private void LoadFromExchange()
		{
			_updatingModel = true;

			try
			{
				ExchangeRusName = Exchange.RusName;
				ExchangeEngName = Exchange.EngName;
				CountryCode = Exchange.CountryCode;
				ExchangeTimeZone = Exchange.TimeZoneInfo;
			}
			finally
			{
				_updatingModel = false;
			}
		}

		private void Reset()
		{
			_updatingModel = true;

			try
			{
				Exchange = null;
				ExchangeName = ExchangeRusName = ExchangeEngName = string.Empty;
				CountryCode = null;
				ExchangeTimeZone = null;
				IsNew = true;
			}
			finally
			{
				_updatingModel = false;
			}
		}

		#region exchange parameters

		public Exchange Exchange
		{
			get { return _exchange; }
			private set { SetField(ref _exchange, value, () => Exchange, vmDataChanged: false); }
		}

		public string ExchangeName { get; private set; }

		public bool IsNew
		{
			get { return _isNew; }
			private set { SetField(ref _isNew, value, () => IsNew, vmDataChanged: false); }
		}

		public string ExchangeRusName
		{
			get { return _exchangeRusName; }
			set { SetField(ref _exchangeRusName, value, () => ExchangeRusName); }
		}

		public string ExchangeEngName
		{
			get { return _exchangeEngName; }
			set { SetField(ref _exchangeEngName, value, () => ExchangeEngName); }
		}

		public CountryCodes? CountryCode
		{
			get { return _countryCode; }
			set { SetField(ref _countryCode, value, () => CountryCode); }
		}

		public TimeZoneInfo ExchangeTimeZone
		{
			get { return _exchangeTimeZone; }
			set { SetField(ref _exchangeTimeZone, value, () => ExchangeTimeZone); }
		}

		public string SaveErrorMessage
		{
			get { return _saveErrorMessage; }
			set { SetField(ref _saveErrorMessage, value, () => SaveErrorMessage, vmDataChanged: false); }
		}

		#endregion

		#region commands implementation

		private void CmdAddNewExchange()
		{
			if (!IsNew || !IsExchangeDataValid())
				return;

			Exchange = CreateExchangeFromData();
			IsNew = false;
			SaveErrorMessage = null;
			Provider.Save(Exchange);
		}

		#endregion

		private bool IsExchangeDataValid()
		{
			if (ExchangeName.IsEmpty())
				SaveErrorMessage = LocalizedStrings.Str1461;
			else if (ExchangeRusName.IsEmpty())
				SaveErrorMessage = LocalizedStrings.Str1462;
			else if (ExchangeEngName.IsEmpty())
				SaveErrorMessage = LocalizedStrings.Str1463;
			else if (CountryCode == null)
				SaveErrorMessage = LocalizedStrings.Str1464;
			else if (ExchangeTimeZone == null)
				SaveErrorMessage = LocalizedStrings.Str1465;
			else
				return true;

			return false;
		}

		public void SaveExchange()
		{
			if (IsNew || Exchange == null || !IsExchangeDataValid())
				return;

			var ex = CreateExchangeFromData();
			ex.Name = Exchange.Name;

			Provider.Save(ex);
		}

		private Exchange CreateExchangeFromData()
		{
			return new Exchange
			{
				Name = ExchangeName,
				RusName = ExchangeRusName,
				EngName = ExchangeEngName,
				CountryCode = CountryCode,
				TimeZoneInfo = ExchangeTimeZone
			};
		}

		private void SetField<T>(ref T field, T value, Expression<Func<T>> selectorExpression, bool vmDataChanged = true)
		{
			var changed = base.SetField(ref field, value, selectorExpression);

			if (!_updatingModel && vmDataChanged && changed)
				DataChanged.SafeInvoke();
		}
	}
}