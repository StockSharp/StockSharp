namespace SpeedTest
{
	using System;
	using System.Windows;

	using Ecng.Collections;
	
	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Quik;
	using StockSharp.SmartCom;

	public partial class NewOrderTest
	{
		private readonly Connector _connector;

		public NewOrderTest(Connector connector, FilterableSecurityProvider securityProvider)
		{
			InitializeComponent();

			_connector = connector;

			Portfolios.Connector = connector;
			Securities.SecurityProvider = securityProvider;
		}

		private void Ok(object sender, RoutedEventArgs e)
		{
			try
			{
				var adapter = ((BasketMessageAdapter)_connector.TransactionAdapter).Portfolios.TryGetValue(Portfolios.SelectedPortfolio.Name);

				var sp = new SpeedTestStrategy(int.Parse(NumberOfTests.Text))
				{
					Connector = _connector,
					Portfolio = Portfolios.SelectedPortfolio,
					Security = Securities.SelectedSecurity,
					Volume = 1,
					TraderName = adapter is QuikMessageAdapter
					             	? "Quik"
									: adapter is SmartComMessageAdapter ? "SmartCom" : "Plaza",
				};

				_connector.RegisterMarketDepth(sp.Security);
				MainWindow.Strategies.Add(sp);
				Close();
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, ex.ToString());
			}
		}
	}
}
