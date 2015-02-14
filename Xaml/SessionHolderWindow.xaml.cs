namespace StockSharp.Xaml
{
	using System;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Окно редактирования настроек подключения <see cref="IMessageSessionHolder"/>.
	/// </summary>
	public partial class SessionHolderWindow
	{
		private IMessageSessionHolder _sessionHolder;
		private IMessageSessionHolder _editableSession;

		/// <summary>
		/// Контейнер для сессии.
		/// </summary>
		public IMessageSessionHolder SessionHolder
		{
			get { return _sessionHolder; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_sessionHolder = value;

				_editableSession = _sessionHolder.GetType().CreateInstanceArgs<IMessageSessionHolder>(new object[] { _sessionHolder.TransactionIdGenerator });
				_editableSession.Load(_sessionHolder.Save());

				SettingsGrid.SelectedObject = _editableSession;
			}
		}

		/// <summary>
		/// Создать <see cref="SessionHolderWindow"/>.
		/// </summary>
		public SessionHolderWindow()
		{
			InitializeComponent();
		}

		private void Test_Click(object sender, RoutedEventArgs e)
		{
			if (!CheckIsValid())
				return;

			BusyIndicator.IsBusy = true;
			Test.IsEnabled = false;

			var connector = new Connector
			{
				MarketDataAdapter = _editableSession.IsMarketDataEnabled
					? _editableSession.CreateMarketDataAdapter()
					: new PassThroughMessageAdapter(_editableSession),

				TransactionAdapter = _editableSession.IsTransactionEnabled
					? _editableSession.CreateTransactionAdapter()
					: new PassThroughMessageAdapter(_editableSession)
			};

			connector.ApplyMessageProcessor(MessageDirections.In, true, true);
			connector.ApplyMessageProcessor(MessageDirections.Out, true, true);

			connector.ExportStarted += () =>
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

			connector.Connected += connector.StartExport;

			Action<Exception> errorHandler = error =>
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

			connector.ConnectionError += errorHandler;
			connector.ExportError += errorHandler;

			connector.Connect();
		}

		private void OkBtn_Click(object sender, RoutedEventArgs e)
		{
			if (!CheckIsValid())
				return;

			SessionHolder.Load(_editableSession.Save());

			DialogResult = true;
		}

		private bool CheckIsValid()
		{
			if (!_editableSession.IsValid)
			{
				new MessageBoxBuilder()
					.Text(LocalizedStrings.Str1562)
					.Owner(this)
					.Error()
					.Show();

				return false;
			}

			if (_editableSession.IsMarketDataEnabled == false && _editableSession.IsTransactionEnabled == false)
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
