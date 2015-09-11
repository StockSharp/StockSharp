namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Windows;

	using Ecng.Serialization;

	using StockSharp.Algo;
	using StockSharp.Messages;

	/// <summary>
	/// The window for new connections creating <see cref="IMessageAdapter"/>.
	/// </summary>
	public partial class MessageAdaptersWindow
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MessageAdaptersWindow"/>.
		/// </summary>
		public MessageAdaptersWindow()
		{
			InitializeComponent();
		}

		/// <summary>
		/// The connection status check event.
		/// </summary>
		public event Func<ConnectionStates> CheckConnectionState
		{
			add { ConnectorsPanel.CheckConnectionState += value; }
			remove { ConnectorsPanel.CheckConnectionState -= value; }
		}

		/// <summary>
		/// Auto connect.
		/// </summary>
		public bool AutoConnect
		{
			get { return ConnectorsPanel.AutoConnect; }
			set { ConnectorsPanel.AutoConnect = value; }
		}

		private BasketMessageAdapter _adapter;

		/// <summary>
		/// Adapter aggregator.
		/// </summary>
		public BasketMessageAdapter Adapter
		{
			get { return _adapter; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				if (_adapter == value)
					return;

				_adapter = value;

				var clone = new BasketMessageAdapter(_adapter.TransactionIdGenerator);
				clone.Load(_adapter.Save());
				ConnectorsPanel.Adapter = clone;
			}
		}

		/// <summary>
		/// Visual description of available connections.
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
			_adapter.Load(ConnectorsPanel.Adapter.Save());

			DialogResult = true;
			Close();
		}
	}
}