namespace StockSharp.Xaml
{
	using System;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The window for connection settings editing <see cref="IMessageAdapter"/>.
	/// </summary>
	public partial class MessageAdapterWindow
	{
		private IMessageAdapter _adapter;
		private IMessageAdapter _editableAdapter;

		/// <summary>
		/// Adapter to the trading system.
		/// </summary>
		public IMessageAdapter Adapter
		{
			get { return _adapter; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_adapter = value;

				_editableAdapter = _adapter.GetType().CreateInstanceArgs<IMessageAdapter>(new object[] { _adapter.TransactionIdGenerator });
				_editableAdapter.Load(_adapter.Save());

				SettingsGrid.SelectedObject = _editableAdapter;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MessageAdapterWindow"/>.
		/// </summary>
		public MessageAdapterWindow()
		{
			InitializeComponent();
		}

		private void Test_Click(object sender, RoutedEventArgs e)
		{
			if (!CheckIsValid())
				return;

			BusyIndicator.IsBusy = true;
			Test.IsEnabled = false;

			var connector = new Connector();
			connector.Adapter.InnerAdapters.Add(_editableAdapter);

			connector.Connected += () =>
			{
				connector.Dispose();

				GuiDispatcher.GlobalDispatcher.AddSyncAction(() =>
				{
					new MessageBoxBuilder()
						.Text(LocalizedStrings.Str1560)
						.Owner(this)
						.Show();

					BusyIndicator.IsBusy = false;
					Test.IsEnabled = true;
				});
			};

			connector.ConnectionError += error =>
			{
				connector.Dispose();

				GuiDispatcher.GlobalDispatcher.AddSyncAction(() =>
				{
					new MessageBoxBuilder()
						.Text(LocalizedStrings.Str1561 + Environment.NewLine + error)
						.Error()
						.Owner(this)
						.Show();

					BusyIndicator.IsBusy = false;
					Test.IsEnabled = true;
				});
			};

			connector.Connect();
		}

		private void OkBtn_Click(object sender, RoutedEventArgs e)
		{
			if (!CheckIsValid())
				return;

			Adapter.Load(_editableAdapter.Save());

			DialogResult = true;
		}

		private bool CheckIsValid()
		{
			if (!_editableAdapter.IsValid)
			{
				new MessageBoxBuilder()
					.Text(LocalizedStrings.Str1562)
					.Owner(this)
					.Error()
					.Show();

				return false;
			}

			if (_editableAdapter.SupportedMessages.IsEmpty())
			{
				new MessageBoxBuilder()
					.Text(LocalizedStrings.Str1563)
					.Owner(this)
					.Error()
					.Show();

				return false;
			}

			return true;
		}
	}
}