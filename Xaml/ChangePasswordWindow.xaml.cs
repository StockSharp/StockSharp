namespace StockSharp.Xaml
{
	using System;
	using System.Windows;

	using Ecng.Common;

	/// <summary>
	/// Окно для смены пароля.
	/// </summary>
	partial class ChangePasswordWindow
	{
		/// <summary>
		/// Создать <see cref="ChangePasswordWindow"/>.
		/// </summary>
		public ChangePasswordWindow() 
		{
			InitializeComponent();
		}

		/// <summary>
		/// Событие обработки смена пароля.
		/// </summary>
		public Action Process;

		/// <summary>
		/// Обновить результат смены пароля.
		/// </summary>
		/// <param name="result">Результат ввиде текстового сообщения.</param>
		public void UpdateResult(string result)
		{
			Result.Text = result;
			ChangePassword.IsEnabled = true;
		}

		/// <summary>
		/// Текущий пароль.
		/// </summary>
		public string CurrentPassword
		{
			get { return CurrentPasswordCtrl.Password; }
		}

		/// <summary>
		/// Новый пароль.
		/// </summary>
		public string NewPassword
		{
			get { return NewPasswordCtrl.Password; }
		}

		private void ChangePassword_OnClick(object sender, RoutedEventArgs e)
		{
			ChangePassword.IsEnabled = false;
			Process.SafeInvoke();
		}
	}
}
