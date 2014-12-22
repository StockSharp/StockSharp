namespace StockSharp.Hydra.Windows
{
	using System.Collections.ObjectModel;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Xaml.Database;

	using StockSharp.Hydra.Core;

	public partial class DatabaseConnectionWindow
	{
		private readonly ObservableCollection<DatabaseConnectionPair> _connections = new ObservableCollection<DatabaseConnectionPair>();

		public DatabaseConnectionWindow()
		{
			InitializeComponent();

			_connections.AddRange(DatabaseConnectionCache.Instance.AllConnections);

			ConnectionStrings.ItemsSource = _connections;
			ConnectionStrings.SelectedIndex = 0;
		}

		public DatabaseConnectionPair Connection
		{
			get { return (DatabaseConnectionPair)ConnectionStrings.SelectedItem; }
			set { ConnectionStrings.SelectedItem = value; }
		}

		private void ConnectionStrings_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			SettingsGrid.Connection = Connection ?? new DatabaseConnectionPair();
			OkBtn.IsEnabled = Connection != null;
		}

		private void TestBtn_OnClick(object sender, RoutedEventArgs e)
		{
			var connection = SettingsGrid.Connection;

			if (!connection.Test(this))
				return;

			if (Connection != null &&
				Connection.Provider == connection.Provider &&
				Connection.ConnectionString.CompareIgnoreCase(connection.ConnectionString))
				return;

			_connections.Add(connection);
			DatabaseConnectionCache.Instance.AddConnection(connection);
		}
	}
}