#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: SecurityCreateWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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

			if (security.PriceStep == null || security.PriceStep == 0)
			{
				mbBuilder.Text(LocalizedStrings.Str2925).Show();
				return;
			}

			if (security.VolumeStep == null || security.VolumeStep == 0)
			{
				mbBuilder.Text(LocalizedStrings.Str2924).Show();
				return;
			}

			if (security.Board == null)
			{
				mbBuilder.Text(LocalizedStrings.Str2926).Show();
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