namespace StockSharp.Xaml.PropertyGrid
{
	using System;
	using System.Windows;

	using ActiproSoftware.Windows;

	using Ecng.Common;

	/// <summary>
	/// Editor for <see cref="DateTimeOffset"/>.
	/// </summary>
	public partial class DateTimeOffsetEditor
	{
		private TimeSpan _zoneOffset;

		/// <summary>
		/// Initializes a new instance of the <see cref="DateTimeOffsetEditor"/>.
		/// </summary>
		public DateTimeOffsetEditor()
		{
			InitializeComponent();
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="DateTimeOffsetEditor.Offset"/>.
		/// </summary>
		public static readonly DependencyProperty OffsetProperty =
			DependencyProperty.Register("Offset", typeof(DateTimeOffset?),
				typeof(DateTimeOffsetEditor), new UIPropertyMetadata(DateTimeOffset.Now, OnOffsetProperty));

		private static void OnOffsetProperty(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var editor = (DateTimeOffsetEditor)d;
			var offset = (DateTimeOffset?)e.NewValue;

			if (offset == null)
			{
				editor.Value = null;
				editor._zoneOffset = TimeSpan.Zero;
			}
			else
			{
				editor.Value = offset.Value.DateTime;
				editor._zoneOffset = offset.Value.Offset;
			}
		}

		/// <summary>
		/// Time with time zone.
		/// </summary>
		public DateTimeOffset? Offset
		{
			get { return (DateTimeOffset?)GetValue(OffsetProperty); }
			set { SetValue(OffsetProperty, value); }
		}

		private void DateTimeOffsetEditor_OnValueChanged(object sender, PropertyChangedRoutedEventArgs<DateTime?> e)
		{
			Offset = e.NewValue == null ? (DateTimeOffset?)null : e.NewValue.Value.ApplyTimeZone(_zoneOffset);
		}
	}
}