namespace StockSharp.Xaml
{
	using System;
	using System.Collections.ObjectModel;
	using System.Runtime.CompilerServices;
	using System.Windows;
	using System.Windows.Input;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Высплывающее окно для выбора подходящего инструмента.
	/// </summary>
	public partial class AutoCompletePopUp
	{
		/// <summary>
		/// Создать <see cref="AutoCompletePopUp"/>.
		/// </summary>
		public AutoCompletePopUp()
		{
			InitializeComponent();

			Securities = new ObservableCollection<Security>();
			Matches.ItemsSource = Securities;
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> для <see cref="MatchText"/>.
		/// </summary>
		public static readonly DependencyProperty MatchTextProperty = DependencyProperty.Register("MatchText", typeof(string), typeof(AutoCompletePopUp));

		/// <summary>
		/// Текст для подстветки в идентификаторе инструмента <see cref="Security.Id"/>.
		/// </summary>
		public string MatchText { get; set; }

		/// <summary>
		/// Все подходящие инструменты.
		/// </summary>
		public ObservableCollection<Security> Securities { get; private set; }

		/// <summary>
		/// Событие изменения <see cref="SelectedSecurity"/>.
		/// </summary>
		public event Action SecuritySelected;

		/// <summary>
		/// Выбранные инструмент.
		/// </summary>
		public Security SelectedSecurity
		{
			get
			{
				if (Matches.SelectedItem != null)
					return (Security)Matches.SelectedItem;
				else
					return !Securities.IsEmpty() ? Securities[0] : null;
			}
			set
			{
				Matches.SelectedItem = value;
			}
		}

		private void lstMatches_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			SecuritySelected.SafeInvoke();
		}

		/// <summary>
		/// Событие нажатия клавиши.
		/// </summary>
		public event Action<Key> MatchKeyDown;

		private void lstMatches_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Back:
					if (Matches.SelectedIndex != 0)
					{
						Matches.SelectedIndex -= 1;
						Matches.ScrollIntoView(RuntimeHelpers.GetObjectValue(Matches.SelectedItem));
						break;
					}

					e.Handled = true;

					MatchKeyDown.SafeInvoke(e.Key);
					
					break;

				case Key.Tab:
					Matches.SelectedIndex += 1;
					break;

				case Key.Return:
					ppBox.IsOpen = false;

					MatchKeyDown.SafeInvoke(e.Key);

					lstMatches_MouseDoubleClick(sender, null);

					e.Handled = true;

					break;
			}
		}

		/// <summary>
		/// Сфокусировать.
		/// </summary>
		public void DoFocus()
		{
			Matches.Focus();
		}
	}
}