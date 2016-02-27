#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: HelpButton.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System.Diagnostics;
	using System.Windows;

	using Ecng.Common;

	/// <summary>
	/// Help button.
	/// </summary>
	public partial class HelpButton
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="HelpButton"/>.
		/// </summary>
		public HelpButton()
		{
			InitializeComponent();
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="HelpButton.DocUrl"/>.
		/// </summary>
		public static readonly DependencyProperty DocUrlProperty =
			DependencyProperty.Register(nameof(DocUrl), typeof(string), typeof(HelpButton), new PropertyMetadata(null, (o, args) =>
			{
				var btn = (HelpButton)o;
				btn.IsEnabled = !((string)args.NewValue).IsEmpty();
			}));

		/// <summary>
		/// Internet address of help site.
		/// </summary>
		public string DocUrl
		{
			get { return (string)GetValue(DocUrlProperty); }
			set { SetValue(DocUrlProperty, value); }
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="HelpButton.ShowText"/>.
		/// </summary>
		public static readonly DependencyProperty ShowTextProperty =
			DependencyProperty.Register(nameof(ShowText), typeof(bool), typeof(HelpButton), new PropertyMetadata(false, (o, args) =>
			{
				var btn = (HelpButton)o;
				var showText = (bool)args.NewValue;

				btn.ImgCtrl.Visibility = showText ? Visibility.Collapsed : Visibility.Visible;
				btn.TextCtrl.Visibility = !showText ? Visibility.Collapsed : Visibility.Visible;
			}));

		/// <summary>
		/// Show text instead of image. The default is off.
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