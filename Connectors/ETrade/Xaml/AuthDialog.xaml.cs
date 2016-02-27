#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.ETrade.Xaml.ETrade
File: AuthDialog.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
			= DependencyProperty.Register(nameof(VerificationCode), typeof(string), typeof(AuthDialog), new PropertyMetadata(default(string)));

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