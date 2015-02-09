namespace StockSharp.Xaml
{
	using System;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Контрол, активирующий <see cref="SecurityPickerWindow"/>.
	/// </summary>
	public partial class SecurityEditor
	{
		/// <summary>
		/// Команда на удаление выбранного инструмента.
		/// </summary>
		public readonly static RoutedCommand ClearCommand = new RoutedCommand();

		/// <summary>
		/// Создать <see cref="SecurityEditor"/>.
		/// </summary>
		public SecurityEditor()
		{
			InitializeComponent();

			SecurityProvider = ConfigManager.TryGetService<FilterableSecurityProvider>();

			if (SecurityProvider == null)
			{
				ConfigManager.ServiceRegistered += (t, s) =>
				{
					if (typeof(FilterableSecurityProvider) != t)
						return;

					GuiDispatcher.GlobalDispatcher.AddAction(() => SecurityProvider = (FilterableSecurityProvider)s);
				};
			}
		}

		private FilterableSecurityProvider _securityProvider;

		/// <summary>
		/// Поставщик информации об инструментах.
		/// </summary>
		public FilterableSecurityProvider SecurityProvider
		{
			get { return _securityProvider; }
			set
			{
				if (_securityProvider == value)
					return;

				if (_securityProvider != null)
					SecurityTextBox.ItemsSource = Enumerable.Empty<Security>();

				_securityProvider = value;

				if (_securityProvider == null)
					return;

				var itemsSource = new ObservableCollectionEx<Security>();
				
				lock (_securityProvider.Securities.SyncRoot)
					_securityProvider.Securities.Bind(new ThreadSafeObservableCollection<Security>(itemsSource));
				
				SecurityTextBox.ItemsSource = itemsSource;
			}
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> для <see cref="SelectedSecurity"/>.
		/// </summary>
		public static readonly DependencyProperty SelectedSecurityProperty =
			 DependencyProperty.Register("SelectedSecurity", typeof(Security), typeof(SecurityEditor),
				new FrameworkPropertyMetadata(null, OnSelectedSecurityPropertyChanged));

		private Security _selectedSecurity;

		/// <summary>
		/// Выбранный инструмент.
		/// </summary>
		public Security SelectedSecurity
		{
			get { return _selectedSecurity; }
			set { SetValue(SelectedSecurityProperty, value); }
		}

		/// <summary>
		/// Событие измения выбранного инструмента.
		/// </summary>
		public event Action SecuritySelected;

		private static void OnSelectedSecurityPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
		{
			var editor = (SecurityEditor)source;

			editor._selectedSecurity = (Security)e.NewValue;
			editor.UpdatedControls();
		}

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var wnd = new SecurityPickerWindow
            {
				SecurityProvider = GetSecurityProvider(),
				SelectionMode = DataGridSelectionMode.Single
            };

            if (wnd.ShowModal(this))
            {
                SelectedSecurity = wnd.SelectedSecurity;
            }
        }

		private void UpdatedControls()
		{
            //ButtonSecurity.Content = security == null ? string.Empty : security.Id;
			SecuritySelected.SafeInvoke();
		}

		private void ClearCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			SelectedSecurity = null;
		}

		private FilterableSecurityProvider GetSecurityProvider()
		{
			return SecurityProvider
			       ?? ConfigManager.TryGetService<FilterableSecurityProvider>()
				   ?? (ConfigManager.IsServiceRegistered<IConnector>()
						? new FilterableSecurityProvider(ConfigManager.TryGetService<IConnector>())
						: null);
		}
	}
}