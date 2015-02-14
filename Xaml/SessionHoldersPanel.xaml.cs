namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Linq;
	using System.Threading;
	using System.Windows.Controls;
	using System.Windows.Input;
	using System.Windows.Media.Imaging;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Interop;
	using Ecng.Localization;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.Algo.Testing;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Панель создания новых подключений <see cref="IMessageSessionHolder"/>.
	/// </summary>
	public partial class SessionHoldersPanel
	{
		private sealed class ConnectorRow : NotifiableObject
		{
			public ConnectorRow(ConnectorInfo info, IMessageSessionHolder sessionHolder)
			{
				if (info == null)
					throw new ArgumentNullException("info");

				if (sessionHolder == null)
					throw new ArgumentNullException("sessionHolder");

				Info = info;
				SessionHolder = sessionHolder;
			}

			public ConnectorInfo Info { get; private set; }

			public IMessageSessionHolder SessionHolder { get; private set; }

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
				get { return SessionHolder.ToString(); }
			}

			public void Refresh()
			{
				NotifyChanged("Description");
				NotifyChanged("IsTransactionEnabled");
				NotifyChanged("IsMarketDataEnabled");
			}
		}

		private sealed class ConnectorInfoList : BaseList<ConnectorInfo>
		{
			private readonly SessionHoldersPanel _parent;
			private readonly Languages _language;
			private readonly Dictionary<ConnectorInfo, MenuItem> _items = new Dictionary<ConnectorInfo, MenuItem>();

			public ConnectorInfoList(SessionHoldersPanel parent)
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
					var sessionHolder = item.SessionHolderType.CreateInstanceArgs<IMessageSessionHolder>(new object[] { _parent.SessionHolder.TransactionIdGenerator });

					//sessionHolder.IsMarketDataEnabled = sessionHolder.MarketDataAdapterType != null;
					//sessionHolder.IsTransactionEnabled = sessionHolder.TransactionAdapterType != null;

					var wnd = new SessionHolderWindow
					{
						SessionHolder = sessionHolder,
					};

					if (!wnd.ShowModal(_parent))
						return;

					var row = new ConnectorRow(item, wnd.SessionHolder);

					_parent.SessionHolder.InnerSessions.Add(wnd.SessionHolder, 0);

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
		/// <see cref="RoutedCommand"/> на удаление подключения.
		/// </summary>
		public static readonly RoutedCommand RemoveSessionCommand = new RoutedCommand();

		/// <summary>
		/// <see cref="RoutedCommand"/> на включение подключения.
		/// </summary>
		public static readonly RoutedCommand EnableSessionCommand = new RoutedCommand();

		private readonly ObservableCollection<ConnectorRow> _connectorRows = new ObservableCollection<ConnectorRow>();

		/// <summary>
		/// Создать <see cref="SessionHoldersPanel"/>.
		/// </summary>
		public SessionHoldersPanel()
		{
			InitializeComponent();

			ConnectorsGrid.ItemsSource = _connectorRows;
			_connectorsInfo = new ConnectorInfoList(this);
		}

		/// <summary>
		/// Авто-подключение.
		/// </summary>
		public bool AutoConnect
		{
			get { return AutoConnectCtrl.IsChecked == true; }
			set { AutoConnectCtrl.IsChecked = value; }
		}

		private BasketSessionHolder _sessionHolder;

		/// <summary>
		/// Сессия-агрегатор.
		/// </summary>
		public BasketSessionHolder SessionHolder
		{
			get { return _sessionHolder; }
			set
			{
				_sessionHolder = value;

				_connectorRows.Clear();

				if (_sessionHolder == null)
					return;

				// TODO добавить панель настроек для эмуляционной сессии
				var sessions = _sessionHolder.InnerSessions.Where(s => !(s is HistorySessionHolder));

				_connectorRows.AddRange(sessions.Select(s => new ConnectorRow(GetInfo(s), s) { IsEnabled = _sessionHolder.InnerSessions[s] != -1 }));
			}
		}

		private readonly IList<ConnectorInfo> _connectorsInfo;

		/// <summary>
		/// Визуальное описание доступных подключений.
		/// </summary>
		public IList<ConnectorInfo> ConnectorsInfo
		{
			get { return _connectorsInfo; }
		}

		/// <summary>
		/// Событие изменения настроек.
		/// </summary>
		public event Action ConnectorsChanged;

		/// <summary>
		/// Событие проверки состояния соединения
		/// </summary>
		public event Func<ConnectionStates> CheckConnectionState;

		private ConnectorRow SelectedInfo
		{
			get { return ConnectorsGrid != null ? (ConnectorRow)ConnectorsGrid.SelectedItem : null; }
		}

		private ConnectorInfo GetInfo(IMessageSessionHolder sessionHolder)
		{
			if (sessionHolder == null)
				throw new ArgumentNullException("sessionHolder");

			var info = ConnectorsInfo.FirstOrDefault(i => i.SessionHolderType.IsInstanceOfType(sessionHolder));

			if (info == null)
				throw new ArgumentException(LocalizedStrings.Str1553Params.Put(sessionHolder.GetType()), "sessionHolder");

			return info;
		}

		private bool CheckConnected(string message)
		{
			if (!SelectedInfo.IsEnabled || CheckConnectionState == null)
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

		private void ExecutedRemoveSession(object sender, ExecutedRoutedEventArgs e)
		{
			if (!CheckConnected(LocalizedStrings.Str1554))
				return;

			SessionHolder.InnerSessions.Remove(SelectedInfo.SessionHolder);
			_connectorRows.Remove(SelectedInfo);
			ConnectorsChanged.SafeInvoke();
		}

		private void CanExecuteRemoveSession(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedInfo != null;
		}

		private void ConnectorsGrid_DoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (SelectedInfo == null)
				return;

			if (!CheckConnected(LocalizedStrings.Str1555))
				return;

			var wnd = new SessionHolderWindow
			{
				SessionHolder = SelectedInfo.SessionHolder
			};

			if (!wnd.ShowModal(this))
				return;

			SelectedInfo.Refresh();

			ConnectorsChanged.SafeInvoke();
		}

		private void ConnectorsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ChangeDisableEnableIcon(SelectedInfo != null && SelectedInfo.IsEnabled);
		}

		private void ExecutedEnableSession(object sender, ExecutedRoutedEventArgs e)
		{
			if (!CheckConnected(LocalizedStrings.Str1556))
				return;

			SelectedInfo.IsEnabled = !SelectedInfo.IsEnabled;

			SessionHolder.InnerSessions[SelectedInfo.SessionHolder] = SelectedInfo.IsEnabled ? 0 : -1;

			ChangeDisableEnableIcon(SelectedInfo.IsEnabled);
			ConnectorsChanged.SafeInvoke();
		}

		private void CanExecuteEnableSession(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedInfo != null;
		}

		private void ChangeDisableEnableIcon(bool isEnabled)
		{
			var bmp = EnableImg;
			bmp.Source = new BitmapImage(new Uri("pack://application:,,,/StockSharp.Xaml;component/Images/" + (isEnabled ? "disabled.png" : "enabled.png")));
			bmp.InvalidateMeasure();
			bmp.InvalidateVisual();
			bmp.SetToolTip(isEnabled ? LocalizedStrings.Str1557 : LocalizedStrings.Str1558);
		}
	}

	/// <summary>
	/// Информация о подключении.
	/// </summary>
	public class ConnectorInfo
	{
		/// <summary>
		/// Название подключения.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Описание подключения.
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// Описание подключения.
		/// </summary>
		public string Category { get; set; }

		/// <summary>
		/// Тип контейнера для сессии.
		/// </summary>
		public Type SessionHolderType { get; private set; }

		/// <summary>
		/// Уровень логирования.
		/// </summary>
		public LogLevels LogLevel { get; private set; }

		/// <summary>
		/// Целевая аудитория.
		/// </summary>
		public Languages PreferLanguage { get; private set; }

		/// <summary>
		/// Платформа.
		/// </summary>
		public Platforms Platform { get; private set; }

		/// <summary>
		/// Создать <see cref="ConnectorInfo"/>.
		/// </summary>
		/// <param name="sessionHolderType">Тип контейнера для сессии.</param>
		/// <param name="logLevel">Уровень логирования.</param>
		public ConnectorInfo(Type sessionHolderType, LogLevels logLevel = LogLevels.Inherit)
		{
			if (sessionHolderType == null)
				throw new ArgumentNullException("sessionHolderType");

			Name = sessionHolderType.GetDisplayName();
			Description = sessionHolderType.GetDescription();
			Category = sessionHolderType.GetCategory(LocalizedStrings.Str1559);
			SessionHolderType = sessionHolderType;
			LogLevel = logLevel;

			var targetPlatform = sessionHolderType.GetAttribute<TargetPlatformAttribute>();
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