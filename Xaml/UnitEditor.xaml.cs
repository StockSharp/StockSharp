namespace StockSharp.Xaml
{
	using System;
	using System.Windows;

	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// Контрол для редактирования объекта класса <see cref="Unit"/>.
	/// </summary>
	public partial class UnitEditor
	{
		/// <summary>
		/// Создать объект класса <see cref="UnitEditor"/>.
		/// </summary>
		public UnitEditor()
		{
			InitializeComponent();
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> для <see cref="Value"/>.
		/// </summary>
		public static readonly DependencyProperty ValueProperty =
			 DependencyProperty.Register("Value", typeof(Unit), typeof(UnitEditor), new FrameworkPropertyMetadata(null, OnValuePropertyChanged));

		/// <summary>
		/// Значение, которое необходимо редактировать визуально.
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
		/// Событие изменения редактируемого значения.
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