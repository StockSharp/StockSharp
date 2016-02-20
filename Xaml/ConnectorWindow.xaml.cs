#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: ConnectorWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Linq;
	using System.Threading;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;
	using System.Windows.Media.Imaging;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Localization;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// The window for new connections creating <see cref="IMessageAdapter"/>.
	/// </summary>
	public partial class ConnectorWindow
	{
		private sealed class GridRow : NotifiableObject
		{
			public GridRow(ConnectorInfo info, IMessageAdapter adapter)
			{
				if (info == null)
					throw new ArgumentNullException(nameof(info));

				if (adapter == null)
					throw new ArgumentNullException(nameof(adapter));

				Info = info;
				Adapter = adapter;
			}

			public ConnectorInfo Info { get; private set; }

			public IMessageAdapter Adapter { get; }

			public bool IsTransactionEnabled => Adapter.IsMessageSupported(MessageTypes.OrderRegister);

			public bool IsMarketDataEnabled => Adapter.IsMessageSupported(MessageTypes.MarketData) || Adapter.IsMessageSupported(MessageTypes.SecurityLookup);

			private bool _isEnabled = true;

			public bool IsEnabled
			{
				get { return _isEnabled; }
				set
				{
					_isEnabled = value;
					NotifyChanged(nameof(IsEnabled));
				}
			}

			public string Description => Adapter.ToString();
			public Uri Icon => Adapter.GetType().GetIconUrl();

			public void Refresh()
			{
				NotifyChanged(nameof(IsTransactionEnabled));
				NotifyChanged(nameof(IsMarketDataEnabled));
			}
		}

		private sealed class ConnectorInfoList : BaseList<ConnectorInfo>
		{
			private readonly ConnectorWindow _parent;
			private readonly Languages _language;
			private readonly Dictionary<ConnectorInfo, MenuItem> _items = new Dictionary<ConnectorInfo, MenuItem>();

			public ConnectorInfoList(ConnectorWindow parent)
			{
				if (parent == null)
					throw new ArgumentNullException(nameof(parent));

				_parent = parent;
				_language = Thread.CurrentThread.CurrentCulture.Name == "en-US" ? Languages.English : Languages.Russian;
			}

			private ItemCollection Items => _parent.ConnectionsMenu.Items;

			protected override bool OnAdding(ConnectorInfo item)
			{
				var mi = new MenuItem
				{
					Header = item.Name,
					ToolTip = item.Description
				};

				mi.Click += (sender, args) =>
				{
					var adapter = item.AdapterType.CreateInstanceArgs<IMessageAdapter>(new object[] { _parent.Adapter.TransactionIdGenerator });
					var row = new GridRow(item, adapter/*, GetInnerAdapter(adapter)*/);

					_parent.Adapter.InnerAdapters[adapter] = 0;

					_parent._connectorRows.Add(row);
					//_parent.ConnectorsChanged.SafeInvoke();
				};

				_items.Add(item, mi);

				var separator = Items
					.OfType<HeaderedSeparator>()
					.FirstOrDefault(s => s.Header == item.Category);

				if (separator == null)
				{
					var key = item.PreferLanguage == _language ? -1 : (int)item.PreferLanguage;

					separator = new HeaderedSeparator
					{
						Header = item.Category,
						Tag = key
					};

					var prev = Items.OfType<HeaderedSeparator>().FirstOrDefault();
					if (prev != null && key >= (int)prev.Tag)
					{
						var index = Items
							.OfType<Control>()
							.Select((i, idx) => new { Item = i, Index = idx })
							.FirstOrDefault(p => p.Item.Tag != null && (int)p.Item.Tag > key);

						if (index == null)
							Items.Add(separator);
						else
							Items.Insert(index.Index, separator);
					}
					else
						Items.Insert(0, separator);
				}

				var categoryIndex = Items.IndexOf(separator);

				var categoryItems = Items
					.Cast<object>()
					.Skip(categoryIndex + 1)
					.TakeWhile(o => !(o is HeaderedSeparator))
					.ToArray();

				Items.Insert(categoryIndex + categoryItems.Length + 1, mi);

				return base.OnAdding(item);
			}

			protected override bool OnRemoving(ConnectorInfo item)
			{
				Items.Remove(_items[item]);
				_items.Remove(item);
				return base.OnRemoving(item);
			}

			protected override bool OnClearing()
			{
				Items.Clear();
				_items.Clear();
				return base.OnClearing();
			}
		}

		/// <summary>
		/// <see cref="RoutedCommand"/> for the connection removal.
		/// </summary>
		public static readonly RoutedCommand RemoveCommand = new RoutedCommand();

		/// <summary>
		/// <see cref="RoutedCommand"/> to enable connection.
		/// </summary>
		public static readonly RoutedCommand EnableCommand = new RoutedCommand();

		private readonly ObservableCollection<GridRow> _connectorRows = new ObservableCollection<GridRow>();

		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectorWindow"/>.
		/// </summary>
		public ConnectorWindow()
		{
			InitializeComponent();

			ConnectorsGrid.ItemsSource = _connectorRows;
			ConnectorsInfo = new ConnectorInfoList(this);
		}

		/// <summary>
		/// Auto connect.
		/// </summary>
		public bool AutoConnect
		{
			get { return AutoConnectCtrl.IsChecked == true; }
			set { AutoConnectCtrl.IsChecked = value; }
		}

		private BasketMessageAdapter _adapter;

		/// <summary>
		/// Adapter aggregator.
		/// </summary>
		public BasketMessageAdapter Adapter
		{
			get
			{
				return _adapter;
			}
			set
			{
				if (_adapter == value)
					return;

				_adapter = value;
				_connectorRows.Clear();

				if (_adapter == null)
					return;

				_connectorRows.AddRange(_adapter.InnerAdapters.Select(CreateRow));
			}
		}

		//private static IMessageAdapter GetInnerAdapter(IMessageAdapter adapter)
		//{
		//	while (true)
		//	{
		//		var wrapper = adapter as IMessageAdapterWrapper;
		//		if (wrapper == null)
		//			break;

		//		adapter = wrapper.InnerAdapter;
		//	}

		//	return adapter;
		//}

		private GridRow CreateRow(IMessageAdapter adapter)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			var innerAdapter = adapter.Clone();//GetInnerAdapter(adapter);

			var info = ConnectorsInfo.FirstOrDefault(i => i.AdapterType.IsInstanceOfType(innerAdapter));

			if (info == null)
				throw new ArgumentException(LocalizedStrings.Str1553Params.Put(innerAdapter.GetType()), nameof(adapter));

			return new GridRow(info, adapter/*, innerAdapter*/) { IsEnabled = Adapter.InnerAdapters[adapter] != -1 };
		}

		/// <summary>
		/// Visual description of available connections.
		/// </summary>
		public IList<ConnectorInfo> ConnectorsInfo { get; }

		///// <summary>
		///// The settings change event.
		///// </summary>
		//public event Action ConnectorsChanged;

		///// <summary>
		///// The connection status check event.
		///// </summary>
		//public event Func<ConnectionStates> CheckConnectionState;

		private GridRow SelectedRow => (GridRow)ConnectorsGrid?.SelectedItem;

		private IEnumerable<GridRow> SelectedRows => ConnectorsGrid?.SelectedItems.Cast<GridRow>() ?? Enumerable.Empty<GridRow>();

		//private bool CheckConnected(string message)
		//{
		//	if (!SelectedRow.IsEnabled || CheckConnectionState == null)
		//		return true;

		//	var connectionState = CheckConnectionState();

		//	if (connectionState == ConnectionStates.Disconnected || connectionState == ConnectionStates.Failed)
		//		return true;

		//	new MessageBoxBuilder()
		//		.Owner(this)
		//		.Warning()
		//		.Text(message)
		//		.Show();

		//	return false;
		//}

		private void ExecutedRemove(object sender, ExecutedRoutedEventArgs e)
		{
			//if (!CheckConnected(LocalizedStrings.Str1554))
			//	return;

			foreach (var row in SelectedRows.ToArray())
			{
				Adapter.InnerAdapters.Remove(row.Adapter);
				_connectorRows.Remove(row);
			}

			//ConnectorsChanged.SafeInvoke();
		}

		private void CanExecuteRemove(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedRow != null;
		}

		//private void ConnectorsGrid_DoubleClick(object sender, MouseButtonEventArgs e)
		//{
		//	if (SelectedRow == null)
		//		return;

		//	if (!CheckConnected(LocalizedStrings.Str1555))
		//		return;

		//	var wnd = new MessageAdapterWindow
		//	{
		//		Adapter = SelectedRow.Adapter
		//	};

		//	if (!wnd.ShowModal(this))
		//		return;

		//	SelectedRow.Refresh();

		//	ConnectorsChanged.SafeInvoke();
		//}

		private void ConnectorsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var row = SelectedRow;

			if (row == null)
			{
				SupportedMessages.Adapter = null;
				ChangeDisableEnableIcon(false);
				PropertyGrid.SelectedObject = null;
				HelpButton.DocUrl = null;
				AdapterButtons.IsEnabled = false;
			}
			else
			{
				SupportedMessages.Adapter = row.Adapter;
				ChangeDisableEnableIcon(row.IsEnabled);
				PropertyGrid.SelectedObject = row.Adapter;
				HelpButton.DocUrl = row.Adapter.GetType().GetDocUrl();
				AdapterButtons.IsEnabled = true;
			}
		}

		private void ExecutedEnable(object sender, ExecutedRoutedEventArgs e)
		{
			//if (!CheckConnected(LocalizedStrings.Str1556))
			//	return;

			foreach (var row in SelectedRows)
			{
				row.IsEnabled = !row.IsEnabled;
				Adapter.InnerAdapters[row.Adapter] = row.IsEnabled ? 0 : -1;
			}

			ChangeDisableEnableIcon(SelectedRow.IsEnabled);
			//ConnectorsChanged.SafeInvoke();
		}

		private void CanExecuteEnable(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedRow != null;
		}

		private void ChangeDisableEnableIcon(bool isEnabled)
		{
			var bmp = EnableImg;
			bmp.Source = new BitmapImage(new Uri("pack://application:,,,/StockSharp.Xaml;component/Images/" + (isEnabled ? "disabled.png" : "enabled.png")));
			bmp.InvalidateMeasure();
			bmp.InvalidateVisual();
			bmp.SetToolTip(isEnabled ? LocalizedStrings.Str1557 : LocalizedStrings.Str1558);
		}

		private bool CheckIsValid(IEnumerable<IMessageAdapter> adapters)
		{
			foreach (var adapter in adapters)
			{
				if (!adapter.IsValid)
				{
					new MessageBoxBuilder()
						.Text(LocalizedStrings.Str1562)
						.Caption(adapter.Name)
						.Owner(this)
						.Error()
						.Show();

					return false;
				}

				if (adapter.SupportedMessages.IsEmpty())
				{
					new MessageBoxBuilder()
						.Text(LocalizedStrings.Str1563)
						.Caption(adapter.Name)
						.Owner(this)
						.Error()
						.Show();

					return false;
				}
			}

			return true;
		}

		private void Test_Click(object sender, RoutedEventArgs e)
		{
			var adapters = SelectedRows.Select(r => r.Adapter).ToArray();

			if (!CheckIsValid(adapters))
				return;

			BusyIndicator.IsBusy = true;
			Test.IsEnabled = false;

			var connector = new Connector();
			connector.Adapter.InnerAdapters.AddRange(adapters);

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

		//private BasketMessageAdapter _adapter;

		///// <summary>
		///// Adapter aggregator.
		///// </summary>
		//public BasketMessageAdapter Adapter
		//{
		//	get { return _adapter; }
		//	set
		//	{
		//		if (value == null)
		//			throw new ArgumentNullException("value");

		//		if (_adapter == value)
		//			return;

		//		_adapter = value;

		//		var clone = new BasketMessageAdapter(_adapter.TransactionIdGenerator);
		//		clone.InnerAdapters.AddRange(_adapter.InnerAdapters.Select(a => (IMessageAdapter)a.Clone()));
		//		clone.Load(_adapter.Save());
		//		ConnectorsPanel.Adapter = clone;
		//	}
		//}

		private void ProxySettings_OnClick(object sender, RoutedEventArgs e)
		{
			BaseApplication.EditProxySettings(this);
		}

		private void Ok_OnClick(object sender, RoutedEventArgs e)
		{
			var adapters = _connectorRows.Where(r => r.IsEnabled).Select(r => r.Adapter).ToArray();

			if (!CheckIsValid(adapters))
				return;

			DialogResult = true;
		}

		private void SupportedMessages_OnSelectedChanged()
		{
			SelectedRow?.Refresh();
		}
	}
}