namespace StockSharp.ETrade.Xaml
{
	using System;
	using System.Diagnostics;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Xaml;
	using StockSharp.Localization;

	/// <summary>
	/// Окно авторизации E*TRADE.
	/// </summary>
	public partial class AuthDialog
	{
		private readonly string _authUrl;

		/// <summary>
		/// <see cref="DependencyProperty"/> для <see cref="VerificationCode"/>.
		/// </summary>
		public static readonly DependencyProperty VerificationCodeProperty
			= DependencyProperty.Register("VerificationCode", typeof(string), typeof(AuthDialog), new PropertyMetadata(default(string)));

		/// <summary>
		/// Создать <see cref="AuthDialog"/>.
		/// </summary>
		public AuthDialog(string authUrl)
		{
			if (authUrl.IsEmpty())
				throw new ArgumentNullException("authUrl");

			_authUrl = authUrl;
			InitializeComponent();
		}

		/// <summary>
		/// Код верификации для продолжения процесса авторизации в системе E*TRADE.
		/// </summary>
		public string VerificationCode
		{
			get { return (string)GetValue(VerificationCodeProperty); }
			set { SetValue(VerificationCodeProperty, value); }
		}

		private void ButtonContinue_Click(object sender, RoutedEventArgs e)
		{
			var code = VerificationCode.IsEmpty() ? null : VerificationCode.Trim();

			if (code.IsEmpty())
			{
				new MessageBoxBuilder()
					.Owner(this)
					.Caption(LocalizedStrings.Str152)
					.Text(LocalizedStrings.Str3377)
					.Error()
					.Show();

				return;
			}

			VerificationCode = code;

			DialogResult = true;
			Close();
		}

		private void ButtonGetCode_Click(object sender, RoutedEventArgs e)
		{
			Process.Start(_authUrl);
		}
	}
}