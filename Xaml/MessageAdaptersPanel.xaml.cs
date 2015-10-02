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
	using Ecng.Interop;
	using Ecng.Localization;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The panel for the new connections creating <see cref="IMessageAdapter"/>.
	/// </summary>
	public partial class MessageAdaptersPanel
	{
		private class SupportedMessage
		{
			private readonly IMessageAdapter _adapter;

			public SupportedMessage(IMessageAdapter adapter, MessageTypes type)
			{
				_adapter = adapter;
				Type = type;
				Name = type.GetDisplayName();
			}

			public MessageTypes Type { get; private set; }
			public string Name { get; private set; }

			public event Action SelectedChanged;

			public bool IsSelected
			{
				get { return _adapter.IsMessageSupported(Type); }
				set
				{
					if (value)
						_adapter.AddSupportedMessage(Type);
					else
						_adapter.RemoveSupportedMessage(Type);

					SelectedChanged.SafeInvoke();
				}
			}
		}

		private sealed class GridRow : NotifiableObject
		{
			public GridRow(ConnectorInfo info, IMessageAdapter adapter, IMessageAdapter innerAdapter)
			{
				if (info == null)
					throw new ArgumentNullException("info");

				if (adapter == null)
					throw new ArgumentNullException("adapter");

				if (innerAdapter == null)
					throw new ArgumentNullException("innerAdapter");

				Info = info;
				Adapter = adapter;
				InnerAdapter = innerAdapter;
				SupportedMessages = innerAdapter.GetType().CreateInstance<IMessageAdapter>(innerAdapter.TransactionIdGenerator).SupportedMessages.Select(m => new SupportedMessage(adapter, m)).ToArray();
				SupportedMessages.ForEach(m => m.SelectedChanged += () =>
				{
					NotifyChanged("IsTransactionEnabled");
					NotifyChanged("IsMarketDataEnabled");
				});
				Icon = innerAdapter.GetType().GetIconUrl();
			}

			public ConnectorInfo Info { get; private set; }

			public IMessageAdapter Adapter { get; private set; }
			public IMessageAdapter InnerAdapter { get; private set; }

			public bool IsTransactionEnabled
			{
				get { return Adapter.IsMessageSupported(MessageTypes.OrderRegister); }
			}

			public bool IsMarketDataEnabled
			{
				get { return Adapter.IsMessageSupported(MessageTypes.MarketData) || Adapter.IsMessageSupported(MessageTypes.SecurityLookup); }
			}

			private bool _isEnabled = true;

			public bool IsEnabled
			{
				get { return _isEnabled; }
				set
				{
					_isEnabled = value;
					NotifyChanged("IsEnabled");
				}
			}

			public string Description
			{
				get { return InnerAdapter.ToString(); }
			}

			public Uri Icon { get; private set; }

			//public void Refresh()
			//{
			//	NotifyChanged("Description");
			//	NotifyChanged("IsTransactionEnabled");
			//	NotifyChanged("IsMarketDataEnabled");
			//}

			public SupportedMessage[] SupportedMessages { get; private set; }
		}

		private sealed class ConnectorInfoList : BaseList<ConnectorInfo>
		{
			private readonly MessageAdaptersPanel _parent;
			private readonly Languages _language;
			private readonly Dictionary<ConnectorInfo, MenuItem> _items = new Dictionary<ConnectorInfo, MenuItem>();

			public ConnectorInfoList(MessageAdaptersPanel parent)
			{
				if (parent == null)
					throw new ArgumentNullException("parent");

				_parent = parent;
				_language = Thread.CurrentThread.CurrentCulture.Name == "en-US" ? Languages.English : Languages.Russian;
			}

			private ItemCollection Items
			{
				get { return _parent.ConnectionsMenu.Items; }
			}

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
					var row = new GridRow(item, adapter, _parent.GetInnerAdapter(adapter));

					_parent.Adapter.InnerAdapters[adapter] = 0;

					_parent._connectorRows.Add(row);
					_parent.ConnectorsChanged.SafeInvoke();
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
		private readonly ObservableCollection<SupportedMessage> _supportedMessages = new ObservableCollection<SupportedMessage>();

		/// <summary>
		/// Initializes a new instance of the <see cref="MessageAdaptersPanel"/>.
		/// </summary>
		public MessageAdaptersPanel()
		{
			InitializeComponent();

			ConnectorsGrid.ItemsSource = _connectorRows;
			_connectorsInfo = new ConnectorInfoList(this);

			SupportedMessages.ItemsSource = _supportedMessages;
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
			get { return _adapter; }
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

		private IMessageAdapter GetInnerAdapter(IMessageAdapter adapter)
		{
			while (true)
			{
				var wrapper = adapter as IMessageAdapterWrapper;
				if (wrapper == null)
					break;

				adapter = wrapper.InnerAdapter;
			}

			return adapter;
		}

		private GridRow CreateRow(IMessageAdapter adapter)
		{
			if (adapter == null)
				throw new ArgumentNullException("adapter");

			var innerAdapter = GetInnerAdapter(adapter);

			var info = ConnectorsInfo.FirstOrDefault(i => i.AdapterType.IsInstanceOfType(innerAdapter));

			if (info == null)
				throw new ArgumentException(LocalizedStrings.Str1553Params.Put(innerAdapter.GetType()), "adapter");

			return new GridRow(info, adapter, innerAdapter) { IsEnabled = _adapter.InnerAdapters[adapter] != -1 };
		}

		private readonly IList<ConnectorInfo> _connectorsInfo;

		/// <summary>
		/// Visual description of available connections.
		/// </summary>
		public IList<ConnectorInfo> ConnectorsInfo
		{
			get { return _connectorsInfo; }
		}

		/// <summary>
		/// The settings change event.
		/// </summary>
		public event Action ConnectorsChanged;

		/// <summary>
		/// The connection status check event.
		/// </summary>
		public event Func<ConnectionStates> CheckConnectionState;

		private GridRow SelectedRow
		{
			get { return ConnectorsGrid != null ? (GridRow)ConnectorsGrid.SelectedItem : null; }
		}

		private IEnumerable<GridRow> SelectedRows
		{
			get { return ConnectorsGrid != null ? ConnectorsGrid.SelectedItems.Cast<GridRow>() : Enumerable.Empty<GridRow>(); }
		}

		private bool CheckConnected(string message)
		{
			if (!SelectedRow.IsEnabled || CheckConnectionState == null)
				return true;

			var connectionState = CheckConnectionState();

			if (connectionState == ConnectionStates.Disconnected || connectionState == ConnectionStates.Failed)
				return true;

			new MessageBoxBuilder()
				.Owner(this)
				.Warning()
				.Text(message)
				.Show();

			return false;
		}

		private void ExecutedRemove(object sender, ExecutedRoutedEventArgs e)
		{
			if (!CheckConnected(LocalizedStrings.Str1554))
				return;

			foreach (var row in SelectedRows.ToArray())
			{
				Adapter.InnerAdapters.Remove(row.Adapter);
				_connectorRows.Remove(row);	
			}
			
			ConnectorsChanged.SafeInvoke();
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
			_supportedMessages.Clear();

			var row = SelectedRow;

			if (row == null)
			{
				ChangeDisableEnableIcon(false);
				PropertyGrid.SelectedObject = null;
				HelpButton.DocUrl = null;
				AdapterButtons.IsEnabled = false;
			}
			else
			{
				ChangeDisableEnableIcon(row.IsEnabled);
				PropertyGrid.SelectedObject = row.InnerAdapter;
				HelpButton.DocUrl = row.InnerAdapter.GetType().GetDocUrl();
				AdapterButtons.IsEnabled = true;

				_supportedMessages.AddRange(row.SupportedMessages);
			}
		}

		private void ExecutedEnable(object sender, ExecutedRoutedEventArgs e)
		{
			if (!CheckConnected(LocalizedStrings.Str1556))
				return;

			foreach (var row in SelectedRows)
			{
				row.IsEnabled = !row.IsEnabled;
				Adapter.InnerAdapters[row.Adapter] = row.IsEnabled ? 0 : -1;	
			}

			ChangeDisableEnableIcon(SelectedRow.IsEnabled);
			ConnectorsChanged.SafeInvoke();
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

		private bool CheckIsValid(IMessageAdapter adapter)
		{
			if (!adapter.IsValid)
			{
				new MessageBoxBuilder()
					.Text(LocalizedStrings.Str1562)
					.Owner(this)
					.Error()
					.Show();

				return false;
			}

			if (adapter.SupportedMessages.IsEmpty())
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

		private void Test_Click(object sender, RoutedEventArgs e)
		{
			var adapter = SelectedRow.InnerAdapter;

			if (!CheckIsValid(adapter))
				return;

			BusyIndicator.IsBusy = true;
			Test.IsEnabled = false;

			var connector = new Connector();
			connector.Adapter.InnerAdapters.Add(adapter);

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
	}

	/// <summary>
	/// Information about connection.
	/// </summary>
	public class ConnectorInfo
	{
		/// <summary>
		/// The connection name.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// The connection description.
		/// </summary>
		public string Description { get; private set; }

		/// <summary>
		/// The connection description.
		/// </summary>
		public string Category { get; private set; }

		/// <summary>
		/// The target audience.
		/// </summary>
		public Languages PreferLanguage { get; private set; }

		/// <summary>
		/// Platform.
		/// </summary>
		public Platforms Platform { get; private set; }

		/// <summary>
		/// The type of adapter.
		/// </summary>
		public Type AdapterType { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectorInfo"/>.
		/// </summary>
		/// <param name="adapterType">The type of transaction and market data adapter.</param>
		public ConnectorInfo(Type adapterType)
		{
			if (adapterType == null)
				throw new ArgumentNullException("adapterType");

			if (!typeof(IMessageAdapter).IsAssignableFrom(adapterType))
				throw new ArgumentException("adapterType");

			AdapterType = adapterType;
			Name = adapterType.GetDisplayName();
			Description = adapterType.GetDescription();
			Category = adapterType.GetCategory(LocalizedStrings.Str1559);

			var targetPlatform = adapterType.GetAttribute<TargetPlatformAttribute>();
			if (targetPlatform != null)
			{
				PreferLanguage = targetPlatform.PreferLanguage;
				Platform = targetPlatform.Platform;
			}
			else
			{
				PreferLanguage = Languages.English;
				Platform = Platforms.AnyCPU;
			}
		}
	}
}