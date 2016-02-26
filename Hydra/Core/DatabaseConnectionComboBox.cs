#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Core.CorePublic
File: DatabaseConnectionComboBox.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Core
{
	using System.Collections.ObjectModel;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Collections;
	using Ecng.Xaml;
	using Ecng.Xaml.Database;
	using StockSharp.Localization;

	/// <summary>
	/// Комбо элемент для выбора подключения к базе данных.
	/// </summary>
	public class DatabaseConnectionComboBox : ComboBox
	{
		private class NewConnectionPair : DatabaseConnectionPair
		{
			public override string Title => LocalizedStrings.Str2209;
		}

		private static readonly DatabaseConnectionPair _newConnection = new NewConnectionPair();
		private readonly ObservableCollection<DatabaseConnectionPair> _connections = new ObservableCollection<DatabaseConnectionPair>();
		private int _prevIndex = -1;
 
		/// <summary>
		/// Создать <see cref="DatabaseConnectionComboBox"/>.
		/// </summary>
		public DatabaseConnectionComboBox()
		{
			DisplayMemberPath = nameof(DatabaseConnectionPair.Title);
			ItemsSource = _connections;

			_connections.AddRange(DatabaseConnectionCache.Instance.AllConnections);
			_connections.Add(_newConnection);

			//_prevIndex = SelectedIndex;
		}

		private void AddNewConnection(DatabaseConnectionPair connection)
		{
			DatabaseConnectionCache.Instance.AddConnection(connection);
			_connections.Insert(_connections.Count - 1, connection);
		}

		/// <summary>
		/// Обработчик события смены выбранного элемента.
		/// </summary>
		/// <param name="e">Параметр события.</param>
		protected override void OnSelectionChanged(SelectionChangedEventArgs e)
		{
			base.OnSelectionChanged(e);

			if (SelectedItem == _newConnection)
			{
				var wnd = new DatabaseConnectionCreateWindow();

				if (wnd.ShowModal(this) && !_connections.Contains(wnd.Connection))
				{
					AddNewConnection(wnd.Connection);
					SelectedConnection = wnd.Connection;
				}
				else
				{
					SelectedIndex = _prevIndex;
				}
			}
			else
				SelectedConnection = (DatabaseConnectionPair)SelectedItem;
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> для <see cref="SelectedConnection"/>.
		/// </summary>
		public static readonly DependencyProperty SelectedConnectionProperty =
			DependencyProperty.Register(nameof(SelectedConnection), typeof(DatabaseConnectionPair), typeof(DatabaseConnectionComboBox), new PropertyMetadata(SelectedConnectionPropertyChanged));

		private static void SelectedConnectionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var ctrl = d as DatabaseConnectionComboBox;
			if (ctrl == null)
				return;

			if (e.NewValue == null)
			{
				ctrl.SelectedIndex = -1;
				ctrl._prevIndex = -1;
			}
			else
			{
				var pair = (DatabaseConnectionPair)e.NewValue;

				if (!ctrl._connections.Contains(pair))
					ctrl.AddNewConnection(pair);

				ctrl.SelectedIndex = ctrl._prevIndex = ctrl._connections.IndexOf(pair);
			}
		}

		/// <summary>
		/// Выбранное подключения.
		/// </summary>
		public DatabaseConnectionPair SelectedConnection
		{
			get { return (DatabaseConnectionPair)GetValue(SelectedConnectionProperty); }
			set { SetValue(SelectedConnectionProperty, value); }
		}
	}
}