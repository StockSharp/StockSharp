#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: UnitEditor.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
			 DependencyProperty.Register(nameof(Value), typeof(Unit), typeof(UnitEditor), new FrameworkPropertyMetadata(null, OnValuePropertyChanged));

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