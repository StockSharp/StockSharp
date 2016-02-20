#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: CredentialsWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System.Security;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Common;

	using StockSharp.Community;
	using StockSharp.Localization;

	/// <summary>
	/// The window for editing <see cref="ServerCredentials"/>.
	/// </summary>
	public partial class CredentialsWindow
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CredentialsWindow"/>.
		/// </summary>
		public CredentialsWindow()
		{
			InitializeComponent();
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
			get { return PasswordCtrl.Password.To<SecureString>(); }
			set { PasswordCtrl.Password = value.To<string>(); }
		}

		/// <summary>
		/// Auto login.
		/// </summary>
		public bool AutoLogon
		{
			get { return AutoLogonCtrl.IsChecked == true; }
			set { AutoLogonCtrl.IsChecked = value; }
		}

		private bool _isLoggedIn;

		/// <summary>
		/// Whether the user is logged in.
		/// </summary>
		public bool IsLoggedIn
		{
			get { return _isLoggedIn; }
			set
			{
				_isLoggedIn = value;

				LoginCtrl.IsReadOnly = value;
				PasswordCtrl.IsEnabled = !value;

				Ok.Content = value ? LocalizedStrings.Str1457 : LocalizedStrings.Str1458;
			}
		}

		private void LoginCtrl_OnTextChanged(object sender, TextChangedEventArgs e)
		{
			TryEnableOk();
		}

		private void PasswordCtrl_OnPasswordChanged(object sender, RoutedEventArgs e)
		{
			TryEnableOk();
		}

		private void TryEnableOk()
		{
			Ok.IsEnabled = !Login.IsEmpty() && !Password.IsEmpty();
		}

		private void Ok_OnClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		private void Proxy_OnClick(object sender, RoutedEventArgs e)
		{
			BaseApplication.EditProxySettings(this);
		}
	}
}