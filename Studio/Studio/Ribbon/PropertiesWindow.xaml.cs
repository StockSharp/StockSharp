namespace StockSharp.Studio.Ribbon
{
	using System.Windows;

	using Ecng.Serialization;

	public partial class PropertiesWindow
	{
		public static readonly DependencyProperty SelectedObjectProperty = DependencyProperty.Register("SelectedObject", typeof(IPersistable), typeof(PropertiesWindow),
			new FrameworkPropertyMetadata(null));

		public IPersistable SelectedObject
		{
			get { return (IPersistable)GetValue(SelectedObjectProperty); }
			set { SetValue(SelectedObjectProperty, value); }
		}

		public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(PropertiesWindow),
			new FrameworkPropertyMetadata(false));

		public bool IsReadOnly
		{
			get { return (bool)GetValue(IsReadOnlyProperty); }
			set { SetValue(IsReadOnlyProperty, value); }
		}

		public PropertiesWindow()
		{
			InitializeComponent();
		}
	}
}
