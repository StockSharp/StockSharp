#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Windows.HydraPublic
File: DatabaseConnectionWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Windows
{
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Xaml.Database;

	using StockSharp.Hydra.Core;

	public partial class DatabaseConnectionWindow
	{
		public DatabaseConnectionWindow()
		{
			InitializeComponent();

			ConnectionStrings.SelectedConnection = DatabaseConnectionCache.Instance.AllConnections.FirstOrDefault();
		}

		public DatabaseConnectionPair Connection
		{
			get { return (DatabaseConnectionPair)ConnectionStrings.SelectedItem; }
			set { ConnectionStrings.SelectedItem = value; }
		}

		private void ConnectionStrings_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			SettingsGrid.Connection = Connection ?? new DatabaseConnectionPair();
			TestBtn.IsEnabled = OkBtn.IsEnabled = Connection != null;
		}

		private void TestBtn_OnClick(object sender, RoutedEventArgs e)
		{
			var connection = SettingsGrid.Connection;

			if (!connection.Test(this))
				return;

			//if (Connection != null &&
			//	Connection.Provider == connection.Provider &&
			//	Connection.ConnectionString.CompareIgnoreCase(connection.ConnectionString))
			//	return;

			//DatabaseConnectionCache.Instance.AddConnection(connection);
		}
	}
}