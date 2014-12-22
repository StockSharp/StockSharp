namespace StockSharp.Studio.StrategyRunner
{
	using System.Security;
	using System.Windows;
	using System.Windows.Input;

	using Ecng.Common;

	public partial class SecretWindow
	{
		private const string _fakeMask = "5mmdfxfo56";

		public static readonly RoutedCommand OkCommand = new RoutedCommand();
		public static readonly RoutedCommand CancelCommand = new RoutedCommand();

		public static readonly DependencyProperty SecretProperty = DependencyProperty.Register("Secret", typeof(SecureString), typeof(SecretWindow),
			new PropertyMetadata(default(SecureString), OnPropertyChangedCallback));

		private static void OnPropertyChangedCallback(DependencyObject o, DependencyPropertyChangedEventArgs args)
		{
			var picker = (SecretWindow)o;
			var secret = (SecureString)args.NewValue;

			if (picker.PasswordCtrl.Password.IsEmpty() && secret != null && secret.Length > 0)
			{
				// заполняем поле пароля звездочками
				picker.PasswordCtrl.Password = _fakeMask;
			}
		}

		public SecureString Secret
		{
			get { return (SecureString)GetValue(SecretProperty); }
			set { SetValue(SecretProperty, value); }
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

		private void Ok_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void Ok_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}

		private void Cancel_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		private void PasswordCtrl_OnPasswordChanged(object sender, RoutedEventArgs e)
		{
			if (PasswordCtrl.Password != _fakeMask)
				Secret = PasswordCtrl.Password.To<SecureString>();
		}

		private void Window_OnLoaded(object sender, RoutedEventArgs e)
		{
			PasswordCtrl.Focus();
		}
	}
}
