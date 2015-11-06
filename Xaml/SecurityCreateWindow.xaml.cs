namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Xaml;
	using Ecng.Common;

	using StockSharp.BusinessEntities;

	using StockSharp.Localization;

	/// <summary>
	/// The window for creating and editing <see cref="SecurityCreateWindow.Security"/>.
	/// </summary>
	public partial class SecurityCreateWindow : ISecurityWindow
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SecurityCreateWindow"/>.
		/// </summary>
		public SecurityCreateWindow()
		{
			InitializeComponent();
			Security = new Security
			{
				Board = ExchangeBoard.Nasdaq,
				ExtensionInfo = new Dictionary<object, object>()
			};
		}

		private Func<string, string> _validateId = id => null;

		/// <summary>
		/// The handler checking the entered identifier availability for <see cref="SecurityCreateWindow.Security"/>.
		/// </summary>
		public Func<string, string> ValidateId
		{
			get { return _validateId; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_validateId = value;
			}
		}

		/// <summary>
		/// Security.
		/// </summary>
		public Security Security
		{
			get { return SecurityId.Security; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				SecurityId.Security = value;
				PropertyGrid.SelectedObject = value;

				if (!SecurityId.Text.IsEmpty())
					Title = LocalizedStrings.Str1545Params.Put(value);
			}
		}

		private void SecurityId_TextChanged(object sender, TextChangedEventArgs e)
		{
			Ok.IsEnabled = !SecurityId.Text.IsEmpty();
		}

		private void Ok_Click(object sender, RoutedEventArgs e)
		{
			var security = Security;

			var mbBuilder = new MessageBoxBuilder()
						.Owner(this)
						.Error();

			if (security.PriceStep == null)
			{
				mbBuilder.Text(LocalizedStrings.Str1546).Show();
				return;
			}

			if (security.Board == null)
			{
				mbBuilder.Text(LocalizedStrings.Str1547).Show();
				return;
			}

			if (security.Id.IsEmpty())
			{
				var errorMsg = _validateId(SecurityId.Text) ?? SecurityId.ValidateId();
				if (!errorMsg.IsEmpty())
				{
					mbBuilder.Text(errorMsg).Show();
					return;
				}

				security.Id = SecurityId.Text;
			}
			
			DialogResult = true;
			Close();
		}
	}
}