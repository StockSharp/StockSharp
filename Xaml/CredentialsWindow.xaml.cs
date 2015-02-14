namespace StockSharp.Xaml
{
	using System.Security;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Common;

	using StockSharp.Community;
	using StockSharp.Localization;

	/// <summary>
	/// Окно для редактирования <see cref="ServerCredentials"/>
	/// </summary>
	public partial class CredentialsWindow
	{
		/// <summary>
		/// Создать <see cref="CredentialsWindow"/>.
		/// </summary>
		public CredentialsWindow()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Логин.
		/// </summary>
		public string Login
		{
			get { return LoginCtrl.Text; }
			set { LoginCtrl.Text = value; }
		}

		/// <summary>
		/// Пароль.
		/// </summary>
		public SecureString Password
		{
			get { return PasswordCtrl.Password.To<SecureString>(); }
			set { PasswordCtrl.Password = value.To<string>(); }
		}

		/// <summary>
		/// Входить автоматически.
		/// </summary>
		public bool AutoLogon
		{
			get { return AutoLogonCtrl.IsChecked == true; }
			set { AutoLogonCtrl.IsChecked = value; }
		}

		private bool _isLoggedIn;

		/// <summary>
		/// Находится ли пользователь в залогиненном состоянии.
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
			BaseApplication.EditProxySettigs();
		}
	}
}