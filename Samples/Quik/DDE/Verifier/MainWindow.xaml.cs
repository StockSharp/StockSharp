#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Quik.Verifier.VerifierPublic
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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

			public string Message { get; }
			public bool IsCritical { get; }
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
			get => (QuikTerminal)QuikTerminals.SelectedItem;
			set => QuikTerminals.SelectedItem = value;
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

			if (isDde && CheckDde.IsChecked == false)
				connector.Adapter.InnerAdapters.Remove(connector.MarketDataAdapter);

			connector.Connected += () => this.GuiAsync(() =>
			{
				OnConnect(connector, null);
			});

			connector.ConnectionError += error => this.GuiSync(() => OnConnect(connector, error));

			if (connector.MarketDataAdapter != null)
				connector.Error += error => _settingErrors.Add(new SettingsError(LocalizedStrings.Str3030Params.Put(error.Message), true));	

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