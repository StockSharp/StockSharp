#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: StorageSettingsPanel.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System;
	using System.Security;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using Ookii.Dialogs.Wpf;

	/// <summary>
	/// The visual panel for storage settings.
	/// </summary>
	public partial class StorageSettingsPanel : IPersistable
	{
		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="IsCredentialsEnabled"/>.
		/// </summary>
		public static readonly DependencyProperty IsCredentialsEnabledProperty = DependencyProperty.Register(nameof(IsCredentialsEnabled), typeof(bool), typeof(StorageSettingsPanel), 
			new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

		/// <summary>
		/// <see cref="Login"/> and <see cref="Password"/> are only available for editing. The default value is <see langword="true" />.
		/// </summary>
		public bool IsCredentialsEnabled
		{
			get { return (bool)GetValue(IsCredentialsEnabledProperty); }
			set { SetValue(IsCredentialsEnabledProperty, value); }
		}

		/// <summary>
		/// The remote storage address change event.
		/// </summary>
		public event Action RemotePathChanged;

		/// <summary>
		/// Initializes a new instance of the <see cref="StorageSettingsPanel"/>.
		/// </summary>
		public StorageSettingsPanel()
		{
			InitializeComponent();

			AddressCtrl.TextChanged += (arg1, arg2) => RemotePathChanged.SafeInvoke();
		}

		/// <summary>
		/// Whether local storage is selected.
		/// </summary>
		public bool IsLocal
		{
			get { return IsLocalCtrl.IsChecked == true; }
			set { (value ? IsLocalCtrl : IsRemoteCtrl).IsChecked = true; }
		}

		/// <summary>
		/// The path to local data.
		/// </summary>
		public string Path
		{
			get { return PathCtrl.Text; }
			set { PathCtrl.Text = value; }
		}

		/// <summary>
		/// The remote storage address.
		/// </summary>
		public string Address
		{
			get { return AddressCtrl.Text; }
			set { AddressCtrl.Text = value; }
		}

		/// <summary>
		/// Login.
		/// </summary>
		public string Login
		{
			get { return LoginCtrl.Text; }
			set { LoginCtrl.Text = value; }
		}

		/// <summary>
		/// Password.
		/// </summary>
		public SecureString Password
		{
			get { return PasswordCtrl.Secret; }
			set { PasswordCtrl.Secret = value; }
		}

		private void Browse_Click(object sender, RoutedEventArgs e)
		{
			var dlg = new VistaFolderBrowserDialog();

			if (dlg.ShowDialog(this.GetWindow()) == true)
			{
				PathCtrl.Text = dlg.SelectedPath;
			}
		}

		void IPersistable.Load(SettingsStorage storage)
		{
			IsLocal = storage.GetValue<bool>(nameof(IsLocal));
			Path = storage.GetValue<string>(nameof(Path));
			//IsAlphabetic = storage.GetValue<bool>(nameof(IsAlphabetic));
			Address = storage.GetValue<string>(nameof(Address));
			Login = storage.GetValue<string>(nameof(Login));
			Password = storage.GetValue<SecureString>(nameof(Password));
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(IsLocal), IsLocal);
			storage.SetValue(nameof(Path), Path);
			//storage.SetValue(nameof(IsAlphabetic), IsAlphabetic);
			storage.SetValue(nameof(Address), Address);
			storage.SetValue(nameof(Login), Login);
			storage.SetValue(nameof(Password), Password);
		}
	}
}