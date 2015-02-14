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
	/// Визуальная панель настройки хранилища.
	/// </summary>
	public partial class StorageSettingsPanel : IPersistable
	{
		/// <summary>
		/// <see cref="DependencyProperty"/> для <see cref="IsCredentialsEnabled"/>.
		/// </summary>
		public static readonly DependencyProperty IsCredentialsEnabledProperty = DependencyProperty.Register("IsCredentialsEnabled", typeof(bool), typeof(StorageSettingsPanel), 
			new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

		/// <summary>
		/// <see cref="Login"/> и <see cref="Password"/> доступны только для редактирования.
		/// Значение по умолчанию <see langword="true"/>.
		/// </summary>
		public bool IsCredentialsEnabled
		{
			get { return (bool)GetValue(IsCredentialsEnabledProperty); }
			set { SetValue(IsCredentialsEnabledProperty, value); }
		}

		/// <summary>
		/// Событие изменения адреса удаленного хранилища.
		/// </summary>
		public event Action RemotePathChanged;

		/// <summary>
		/// Создать <see cref="StorageSettingsPanel"/>.
		/// </summary>
		public StorageSettingsPanel()
		{
			InitializeComponent();

			AddressCtrl.TextChanged += (arg1, arg2) => RemotePathChanged.SafeInvoke();
		}

		/// <summary>
		/// Выбрано ли локальное хранилище.
		/// </summary>
		public bool IsLocal
		{
			get { return IsLocalCtrl.IsChecked == true; }
			set { (value ? IsLocalCtrl : IsRemoteCtrl).IsChecked = true; }
		}

		///// <summary>
		///// Использовать ли алфавитный путь к данным.
		///// </summary>
		//public bool IsAlphabetic
		//{
		//	get { return IsAlphabeticCtrl.IsChecked == true; }
		//	set { IsAlphabeticCtrl.IsChecked = value; }
		//}

		/// <summary>
		/// Путь к локальным данным.
		/// </summary>
		public string Path
		{
			get { return PathCtrl.Text; }
			set { PathCtrl.Text = value; }
		}

		/// <summary>
		/// Адрес удаленного хранилища.
		/// </summary>
		public string Address
		{
			get { return AddressCtrl.Text; }
			set { AddressCtrl.Text = value; }
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
			IsLocal = storage.GetValue<bool>("IsLocal");
			Path = storage.GetValue<string>("Path");
			//IsAlphabetic = storage.GetValue<bool>("IsAlphabetic");
			Address = storage.GetValue<string>("Address");
			Login = storage.GetValue<string>("Login");
			Password = storage.GetValue<SecureString>("Password");
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			storage.SetValue("IsLocal", IsLocal);
			storage.SetValue("Path", Path);
			//storage.SetValue("IsAlphabetic", IsAlphabetic);
			storage.SetValue("Address", Address);
			storage.SetValue("Login", Login);
			storage.SetValue("Password", Password);
		}
	}
}