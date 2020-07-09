namespace SampleConnection
{
	using System.ComponentModel;

	using Ecng.Common;
	using Ecng.Configuration;

	using StockSharp.Algo.Storages;

	public partial class MainWindow
	{
		public MainWindow()
		{
			InitializeComponent();
			Instance = this;

			Title = Title.Put("Connections");

			ConfigManager.RegisterService<IExchangeInfoProvider>(new InMemoryExchangeInfoProvider());
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			MainPanel.Close();

			base.OnClosing(e);
		}

		public static MainWindow Instance { get; private set; }
	}
}