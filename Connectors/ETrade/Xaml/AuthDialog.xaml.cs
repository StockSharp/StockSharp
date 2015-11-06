namespace StockSharp.ETrade.Xaml
{
	using System;
	using System.Diagnostics;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Localization;

	/// <summary>
	/// E*TRADE authorization window.
	/// </summary>
	public partial class AuthDialog
	{
		private readonly string _authUrl;

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="VerificationCode"/>.
		/// </summary>
		public static readonly DependencyProperty VerificationCodeProperty
			= DependencyProperty.Register("VerificationCode", typeof(string), typeof(AuthDialog), new PropertyMetadata(default(string)));

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthDialog"/>.
		/// </summary>
		public AuthDialog(string authUrl)
		{
			if (authUrl.IsEmpty())
				throw new ArgumentNullException(nameof(authUrl));

			_authUrl = authUrl;
			InitializeComponent();
		}

		/// <summary>
		/// E*TRADE verification code to continue authorization.
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