namespace StockSharp.Xaml
{
	using System.Diagnostics;
	using System.Windows;

	using Ecng.Common;

	/// <summary>
	/// Кнопка вызова справки.
	/// </summary>
	public partial class HelpButton
	{
		/// <summary>
		/// Создать <see cref="HelpButton"/>.
		/// </summary>
		public HelpButton()
		{
			InitializeComponent();
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> для <see cref="DocUrl"/>.
		/// </summary>
		public static readonly DependencyProperty DocUrlProperty =
			DependencyProperty.Register("DocUrl", typeof(string), typeof(HelpButton), new PropertyMetadata(null, (o, args) =>
			{
				var btn = (HelpButton)o;
				btn.IsEnabled = !((string)args.NewValue).IsEmpty();
			}));

		/// <summary>
		/// Адрес справки в интернете.
		/// </summary>
		public string DocUrl
		{
			get { return (string)GetValue(DocUrlProperty); }
			set { SetValue(DocUrlProperty, value); }
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> для <see cref="ShowText"/>.
		/// </summary>
		public static readonly DependencyProperty ShowTextProperty =
			DependencyProperty.Register("ShowText", typeof(bool), typeof(HelpButton), new PropertyMetadata(false, (o, args) =>
			{
				var btn = (HelpButton)o;
				var showText = (bool)args.NewValue;

				btn.ImgCtrl.Visibility = showText ? Visibility.Collapsed : Visibility.Visible;
				btn.TextCtrl.Visibility = !showText ? Visibility.Collapsed : Visibility.Visible;
			}));

		/// <summary>
		/// Показывать текст вместо картинки. По-умолчанию выключено.
		/// </summary>
		public bool ShowText
		{
			get { return (bool)GetValue(ShowTextProperty); }
			set { SetValue(ShowTextProperty, value); }
		}

		/// <summary>
		/// Called when a <see cref="T:System.Windows.Controls.Button"/> is clicked. 
		/// </summary>
		protected override void OnClick()
		{
			Process.Start(DocUrl);
			base.OnClick();
		}
	}
}