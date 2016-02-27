#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: SecurityEditor.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;

	/// <summary>
	/// The control activating <see cref="SecurityPickerWindow"/>.
	/// </summary>
	public partial class SecurityEditor
	{
		/// <summary>
		/// The command to delete the selected instrument.
		/// </summary>
		public static readonly RoutedCommand ClearCommand = new RoutedCommand();

		/// <summary>
		/// Initializes a new instance of the <see cref="SecurityEditor"/>.
		/// </summary>
		public SecurityEditor()
		{
			InitializeComponent();
		}

		private ThreadSafeObservableCollection<Security> _itemsSource;

		private void UpdateProvider(ISecurityProvider provider)
		{
			if (_securityProvider == provider)
				return;

			if (_securityProvider != null)
			{
				_securityProvider.Added -= AddSecurities;
				_securityProvider.Removed -= RemoveSecurities;
				_securityProvider.Cleared -= ClearSecurities;

				SecurityTextBox.ItemsSource = Enumerable.Empty<Security>();
				_itemsSource = null;
			}

			_securityProvider = provider;

			if (_securityProvider == null)
				return;

			var itemsSource = new ObservableCollectionEx<Security>();

			_itemsSource = new ThreadSafeObservableCollection<Security>(itemsSource);
			_itemsSource.AddRange(_securityProvider.LookupAll());

			_securityProvider.Added += AddSecurities;
			_securityProvider.Removed += RemoveSecurities;
			_securityProvider.Cleared += ClearSecurities;

			SecurityTextBox.ItemsSource = itemsSource;
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="SecurityProvider"/>.
		/// </summary>
		public static readonly DependencyProperty SecurityProviderProperty = DependencyProperty.Register(nameof(SecurityProvider), typeof(ISecurityProvider), typeof(SecurityEditor), new PropertyMetadata(null, (o, args) =>
		{
			var editor = (SecurityEditor)o;
			editor.UpdateProvider((ISecurityProvider)args.NewValue);
		}));

		private ISecurityProvider _securityProvider;

		/// <summary>
		/// The provider of information about instruments.
		/// </summary>
		public ISecurityProvider SecurityProvider
		{
			get { return _securityProvider; }
			set { SetValue(SecurityProviderProperty, value); }
		}

		private void AddSecurities(IEnumerable<Security> securities)
		{
			_itemsSource.AddRange(securities);
		}

		private void RemoveSecurities(IEnumerable<Security> securities)
		{
			_itemsSource.RemoveRange(securities);
		}

		private void ClearSecurities()
		{
			_itemsSource.Clear();
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="SecurityEditor.SelectedSecurity"/>.
		/// </summary>
		public static readonly DependencyProperty SelectedSecurityProperty =
			 DependencyProperty.Register(nameof(SelectedSecurity), typeof(Security), typeof(SecurityEditor),
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
				SecurityProvider = SecurityProvider,
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
	}
}