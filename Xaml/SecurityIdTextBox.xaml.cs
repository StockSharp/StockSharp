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
	/// Текстовое поле, автоматически формирующее идентификатор инструмента на основе его изменяемых полей
	/// <see cref="BusinessEntities.Security.Code"/> и <see cref="BusinessEntities.Security.Board"/>.
	/// </summary>
	public partial class SecurityIdTextBox
	{
		private Security _security;

		/// <summary>
		/// Создать <see cref="SecurityIdTextBox"/>.
		/// </summary>
		public SecurityIdTextBox()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Инструмент.
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
		/// Проверить введенный идентификатор на правильность.
		/// </summary>
		/// <returns>Текст сообщения с ошибкой, или <see langword="null"/>, если ошибки нет.</returns>
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
				return LocalizedStrings.Str1551;

			if (id.IndexOf('@') == (id.Length - 1))
				return LocalizedStrings.Str1552;

			return null;
		}

		private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "Code" || e.PropertyName == "Board")
				RefreshId();
		}

		private void RefreshId()
		{
			Text = Security.Code + "@" + Security.Board;
		}

		/// <summary>
		/// Invoked whenever an unhandled <see cref="E:System.Windows.UIElement.GotFocus"/> event reaches this element in its route.
		/// </summary>
		/// <param name="e">The <see cref="T:System.Windows.RoutedEventArgs"/> that contains the event data.</param>
		protected override void OnGotFocus(RoutedEventArgs e)
		{
			if (!IsReadOnly && Security != null)
				Text = Security.Code;

			base.OnGotFocus(e);
		}

		/// <summary>
		/// Raises the <see cref="E:System.Windows.UIElement.LostFocus"/> event (using the provided arguments).
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