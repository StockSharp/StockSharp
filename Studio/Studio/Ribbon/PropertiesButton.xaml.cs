namespace StockSharp.Studio.Ribbon
{
	using System.Windows;

	using ActiproSoftware.Windows.Controls.Ribbon.Controls;

	using Ecng.Serialization;
	using Ecng.Xaml;

	public partial class PropertiesButton
	{
		public static readonly DependencyProperty SelectedObjectProperty = DependencyProperty.Register("SelectedObject", typeof(IPersistable), typeof(PropertiesButton),
			new FrameworkPropertyMetadata(null));

		public IPersistable SelectedObject
		{
			get { return (IPersistable)GetValue(SelectedObjectProperty); }
			set { SetValue(SelectedObjectProperty, value); }
		}

		public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(PropertiesButton),
			new FrameworkPropertyMetadata(false));

		public bool IsReadOnly
		{
			get { return (bool)GetValue(IsReadOnlyProperty); }
			set { SetValue(IsReadOnlyProperty, value); }
		}

		public PropertiesButton()
		{
			InitializeComponent();
		}

		private void Properties_OnClick(object sender, ExecuteRoutedEventArgs e)
		{
			var wnd = new PropertiesWindow
			{
				SelectedObject = SelectedObject.Clone(),
				IsReadOnly = IsReadOnly
			};

			if (wnd.ShowModal(this))
				SelectedObject.Load(wnd.SelectedObject.Save());
		}
	}
}
