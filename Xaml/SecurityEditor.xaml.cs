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
	/// The control activating <see cref="SecurityPickerWindow"/>.
	/// </summary>
	public partial class SecurityEditor
	{
		/// <summary>
		/// The command to delete the selected instrument.
		/// </summary>
		public readonly static RoutedCommand ClearCommand = new RoutedCommand();

		/// <summary>
		/// Initializes a new instance of the <see cref="SecurityEditor"/>.
		/// </summary>
		public SecurityEditor()
		{
			InitializeComponent();

			SecurityProvider = GetSecurityProvider();

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
		/// The provider of information about instruments.
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
		/// <see cref="DependencyProperty"/> for <see cref="SecurityEditor.SelectedSecurity"/>.
		/// </summary>
		public static readonly DependencyProperty SelectedSecurityProperty =
			 DependencyProperty.Register("SelectedSecurity", typeof(Security), typeof(SecurityEditor),
				new FrameworkPropertyMetadata(null, OnSelectedSecurityPropertyChanged));

		private Security _selectedSecurity;

		/// <summary>
		/// The selected instrument.
		/// </summary>
		public Security SelectedSecurity
		{
			get { return _selectedSecurity; }
			set { SetValue(SelectedSecurityProperty, value); }
		}

		/// <summary>
		/// Gets or sets the text of the currently selected item.
		/// </summary>
		public string Text
		{
			get { return SecurityTextBox.Text; }
			set { SecurityTextBox.Text = value; }
		}

		/// <summary>
		/// The selected instrument change event.
		/// </summary>
		public event Action SecuritySelected;

		private static void OnSelectedSecurityPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
		{
			var editor = (SecurityEditor)source;
			editor.UpdatedControls((Security)e.NewValue);
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

		private void UpdatedControls(Security selectedSecurity)
		{
			_selectedSecurity = selectedSecurity;
			SecurityTextBox.Text = selectedSecurity == null ? string.Empty : selectedSecurity.Id;
			SecuritySelected.SafeInvoke();
		}

		private void ClearCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			SelectedSecurity = null;
		}

		private FilterableSecurityProvider GetSecurityProvider()
		{
			return SecurityProvider
					?? ConfigManager.TryGetService<ISecurityProvider>() as FilterableSecurityProvider
					?? ConfigManager.TryGetService<FilterableSecurityProvider>()
					?? (ConfigManager.IsServiceRegistered<IConnector>()
						? new FilterableSecurityProvider(ConfigManager.TryGetService<IConnector>())
						: null);
		}
	}
}