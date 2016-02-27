#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.StrategyRunner.StrategyRunnerPublic
File: SecretWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.StrategyRunner
{
	using System.Security;
	using System.Windows;

	public partial class SecretWindow
	{
		public SecureString Secret
		{
			get { return PasswordCtrl.Secret; }
			set { PasswordCtrl.Secret = value; }
		}

		public static readonly DependencyProperty SaveSecretProperty = DependencyProperty.Register(nameof(SaveSecret), typeof(bool), typeof(SecretWindow),
			new PropertyMetadata(false));

		public bool SaveSecret
		{
			get { return (bool)GetValue(SaveSecretProperty); }
			set { SetValue(SaveSecretProperty, value); }
		}

		public SecretWindow()
		{
			InitializeComponent();
		}

		private void Window_OnLoaded(object sender, RoutedEventArgs e)
		{
			PasswordCtrl.Focus();
		}
	}
}