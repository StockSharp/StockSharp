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

		public static readonly DependencyProperty SaveSecretProperty = DependencyProperty.Register("SaveSecret", typeof(bool), typeof(SecretWindow),
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