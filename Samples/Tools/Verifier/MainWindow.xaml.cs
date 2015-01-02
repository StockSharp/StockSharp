namespace StockSharp.Quik.Verifier
{
	using System;
	using System.Linq;
	using System.Text;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Quik;

	using StockSharp.Localization;

	public partial class MainWindow
	{
		private sealed class SettingsError
		{
			public SettingsError(string message, bool isCritical)
			{
				Message = message;
				IsCritical = isCritical;
			}

			public string Message { get; private set; }
			public bool IsCritical { get; private set; }
		}

		private readonly ThreadSafeObservableCollection<SettingsError> _settingErrors;

		public MainWindow()
		{
			InitializeComponent();

			var errorSource = new ObservableCollectionEx<SettingsError>();
			Results.ItemsSource = errorSource;
			_settingErrors = new ThreadSafeObservableCollection<SettingsError>(errorSource);

			FindTerminals();
		}

		private void FindTerminals()
		{
			var terminals = QuikTerminal.GetTerminals(false);
			QuikTerminals.ItemsSource = terminals;

			SelectedTerminal = terminals.FirstOrDefault();
		}

		public QuikTerminal SelectedTerminal
		{
			get { return (QuikTerminal)QuikTerminals.SelectedItem; }
			set { QuikTerminals.SelectedItem = value; }
		}

		private void RefreshTerminalsClick(object sender, RoutedEventArgs e)
		{
			FindTerminals();
		}

		private void QuikTerminalsSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var terminal = SelectedTerminal;

			if (terminal != null)
			{
				QuikTitle.Text = terminal.SystemProcess.MainWindowTitle;
				Check.IsEnabled = true;
			}
			else
			{
				QuikTitle.Text = string.Empty;
				Check.IsEnabled = true;
			}
		}

		private void CheckClick(object sender, RoutedEventArgs e)
		{
			var terminal = SelectedTerminal;

			if (terminal.SystemProcess.MainModule == null)
				throw new InvalidOperationException(LocalizedStrings.InvalidProcess);

			OkResult.SetVisibility(false);

			_settingErrors.Clear();

			var isDde = IsDde.IsChecked == true;

			var connector = new QuikTrader(terminal.SystemProcess.MainModule.FileName) { IsDde = isDde };

			var checkDde = isDde && CheckDde.IsChecked == true;

			connector.Connected += () => this.GuiAsync(() =>
			{
				if (checkDde)
					connector.StartExport();

				OnConnect(connector, null);
			});

			connector.ConnectionError += error => this.GuiSync(() => OnConnect(connector, error));

			if (checkDde)
				connector.ProcessDataError += error => _settingErrors.Add(new SettingsError(LocalizedStrings.Str3030Params.Put(error.Message), true));	

			connector.Connect();
		}

		private void OnConnect(QuikTrader trader, Exception connectionError)
		{
			if (connectionError == null)
			{
				_settingErrors.AddRange(trader.Terminal.GetTableSettings()
					.Select(r => new SettingsError(LocalizedStrings.Str3031Params.Put(r.Table.Caption, r.Error.Message), r.IsCritical)));

				if (_settingErrors.Count == 0)
					OkResult.SetVisibility(true);
			}
			else
				MessageBox.Show(this, connectionError.ToString(), "Verifier");

			trader.Dispose();
		}

		private void CopyCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
			e.Handled = true;
		}

		private void CopyExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			var text = new StringBuilder();

			foreach (SettingsError error in Results.SelectedItems)
			{
				text.AppendFormat("{0}.	{1}", error.IsCritical ? LocalizedStrings.Str152 : LocalizedStrings.Warning, error.Message);
				text.AppendLine();
			}

            text.To<string>().TryCopyToClipboard();
			e.Handled = true;
		}
	}
}