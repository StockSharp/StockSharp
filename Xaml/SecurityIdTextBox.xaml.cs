#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: SecurityIdTextBox.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System.ComponentModel;
	using System.IO;
	using System.Linq;
	using System.Windows;

	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Localization;

	/// <summary>
	/// The text field is automatically generating the instrument identifier based on its variable fields <see cref="BusinessEntities.Security.Code"/> and <see cref="BusinessEntities.Security.Board"/>.
	/// </summary>
	public partial class SecurityIdTextBox
	{
		private Security _security;

		/// <summary>
		/// Initializes a new instance of the <see cref="SecurityIdTextBox"/>.
		/// </summary>
		public SecurityIdTextBox()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Security.
		/// </summary>
		public Security Security
		{
			get { return _security; }
			set
			{
				if (value == _security)
					return;

				if (_security != null)
					((INotifyPropertyChanged)_security).PropertyChanged -= OnPropertyChanged;

				_security = value;

				if (_security == null)
				{
					Text = string.Empty;
					return;
				}

				if (_security.Id.IsEmpty())
				{
					((INotifyPropertyChanged)_security).PropertyChanged += OnPropertyChanged;
					RefreshId();
					IsReadOnly = false;
				}
				else
					Text = _security.Id;
			}
		}

		/// <summary>
		/// To check the correctness of the entered identifier.
		/// </summary>
		/// <returns>An error message text, or <see langword="null" /> if no error.</returns>
		public string ValidateId()
		{
			var id = Text;

			var invalidChars = Path.GetInvalidFileNameChars().Where(id.Contains).ToArray();
			if (invalidChars.Any())
			{
				return LocalizedStrings.Str1549Params
					.Put(id, invalidChars.Select(c => c.To<string>()).Join(", "));
			}

			if (id.IndexOf('@') != id.LastIndexOf('@'))
				return LocalizedStrings.Str1550;

			if (id.IndexOf('@') == 0)
				return LocalizedStrings.Str2923;

			if (id.IndexOf('@') == (id.Length - 1))
				return LocalizedStrings.Str2926;

			return null;
		}

		private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "Code" || e.PropertyName == "Board")
				RefreshId();
		}

		private void RefreshId()
		{
			Text = Security.Code + "@" + (Security.Board?.Code ?? string.Empty);
		}

		/// <summary>
		/// Invoked whenever an unhandled <see cref="UIElement.GotFocus"/> event reaches this element in its route.
		/// </summary>
		/// <param name="e">The <see cref="RoutedEventArgs"/> that contains the event data.</param>
		protected override void OnGotFocus(RoutedEventArgs e)
		{
			if (!IsReadOnly && Security != null)
				Text = Security.Code;

			base.OnGotFocus(e);
		}

		/// <summary>
		/// Raises the <see cref="UIElement.LostFocus"/> event (using the provided arguments).
		/// </summary>
		/// <param name="e">Provides data about the event.</param>
		protected override void OnLostFocus(RoutedEventArgs e)
		{
			if (!IsReadOnly && Security != null)
			{
				Security.Code = Text;
				RefreshId();
			}

			base.OnLostFocus(e);
		}
	}
}