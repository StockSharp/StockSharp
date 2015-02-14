namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Windows;

	using Ecng.Serialization;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Окно создания новых подключений <see cref="IMessageSessionHolder"/>.
	/// </summary>
	public partial class SessionHoldersWindow
	{
		private BasketSessionHolder _sourceSessionHolder;

		/// <summary>
		/// Создать <see cref="SessionHoldersWindow"/>.
		/// </summary>
		public SessionHoldersWindow()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Событие проверки состояния соединения
		/// </summary>
		public event Func<ConnectionStates> CheckConnectionState
		{
			add { ConnectorsPanel.CheckConnectionState += value; }
			remove { ConnectorsPanel.CheckConnectionState -= value; }
		}

		/// <summary>
		/// Авто-подключение.
		/// </summary>
		public bool AutoConnect
		{
			get { return ConnectorsPanel.AutoConnect; }
			set { ConnectorsPanel.AutoConnect = value; }
		}

		/// <summary>
		/// Сессия-агрегатор.
		/// </summary>
		public BasketSessionHolder SessionHolder
		{
			get { return _sourceSessionHolder; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				if (_sourceSessionHolder == value)
					return;

				_sourceSessionHolder = value;

				var clone = new BasketSessionHolder(_sourceSessionHolder.TransactionIdGenerator);
				clone.Load(_sourceSessionHolder.Save());
				ConnectorsPanel.SessionHolder = clone;
			}
		}

		/// <summary>
		/// Визуальное описание доступных подключений.
		/// </summary>
		public IList<ConnectorInfo> ConnectorsInfo
		{
			get { return ConnectorsPanel.ConnectorsInfo; }
		}

		private void ProxySettings_OnClick(object sender, RoutedEventArgs e)
		{
			BaseApplication.EditProxySettigs();
		}

		private void Ok_OnClick(object sender, RoutedEventArgs e)
		{
			_sourceSessionHolder.Load(ConnectorsPanel.SessionHolder.Save());

			DialogResult = true;
			Close();
		}
	}
}