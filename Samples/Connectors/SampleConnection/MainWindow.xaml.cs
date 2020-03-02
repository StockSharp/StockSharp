namespace SampleConnection
{
	using System.ComponentModel;

	using Ecng.Common;

	public partial class MainWindow
	{
		public MainWindow()
		{
			InitializeComponent();
			Instance = this;

			Title = Title.Put("Connections");
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			MainPanel.Close();

			base.OnClosing(e);
		}

		public static MainWindow Instance { get; private set; }
	}
}