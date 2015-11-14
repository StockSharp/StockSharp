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
	/// A pop-up window to select a suitable instrument.
	/// </summary>
	public partial class AutoCompletePopUp
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AutoCompletePopUp"/>.
		/// </summary>
		public AutoCompletePopUp()
		{
			InitializeComponent();

			Securities = new ObservableCollection<Security>();
			Matches.ItemsSource = Securities;
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="AutoCompletePopUp.MatchText"/>.
		/// </summary>
		public static readonly DependencyProperty MatchTextProperty = DependencyProperty.Register("MatchText", typeof(string), typeof(AutoCompletePopUp));

		/// <summary>
		/// Text for backlight in the instrument identifier <see cref="Security.Id"/>.
		/// </summary>
		public string MatchText { get; set; }

		/// <summary>
		/// All suitable instruments.
		/// </summary>
		public ObservableCollection<Security> Securities { get; }

		/// <summary>
		/// The change event <see cref="AutoCompletePopUp.SelectedSecurity"/>.
		/// </summary>
		public event Action SecuritySelected;

		/// <summary>
		/// The selected instrument.
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
		/// Key press event.
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
		/// To focus.
		/// </summary>
		public void DoFocus()
		{
			Matches.Focus();
		}
	}
}