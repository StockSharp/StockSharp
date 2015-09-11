namespace StockSharp.Xaml
{
	using System;
	using System.Windows;

	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// The control for the class object <see cref="Unit"/> editing.
	/// </summary>
	public partial class UnitEditor
	{
		/// <summary>
		/// To create an object of the class <see cref="UnitEditor"/>.
		/// </summary>
		public UnitEditor()
		{
			InitializeComponent();
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="UnitEditor.Value"/>.
		/// </summary>
		public static readonly DependencyProperty ValueProperty =
			 DependencyProperty.Register("Value", typeof(Unit), typeof(UnitEditor), new FrameworkPropertyMetadata(null, OnValuePropertyChanged));

		/// <summary>
		/// The value that should be edited visually.
		/// </summary>
		public Unit Value
		{
			get { return (Unit)GetValue(ValueProperty); }
			set
			{
				SetValue(ValueProperty, value);
				ValueChanged.SafeInvoke(Value);
			}
		}

		/// <summary>
		/// The edited value change event.
		/// </summary>
		public event Action<Unit> ValueChanged;

		private static void OnValuePropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
		{
			var unit = (Unit)e.NewValue;
			var editor = (UnitEditor)source;

			editor.Text = unit == null ? string.Empty : unit.ToString();
		}

		private void UnitEditorLostFocus(object sender, RoutedEventArgs e)
		{
			if (Text.IsEmpty())
				Value = null;
			else
			{
				try
				{
					Value = Text.ToUnit();
				}
				catch (FormatException)
				{
				}
			}
		}
	}
}