namespace SampleCurrency
{
	using System.Windows;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.Messages;

	public partial class MainWindow
	{
		public MainWindow()
		{
			InitializeComponent();

			SourceCurrencyType.SetDataSource<CurrencyTypes>();
			TargetCurrencyType.SetDataSource<CurrencyTypes>();

			SourceCurrencyType.SetSelectedValue<CurrencyTypes>(CurrencyTypes.USD);
			TargetCurrencyType.SetSelectedValue<CurrencyTypes>(CurrencyTypes.RUB);
		}

		private void ConvertClick(object sender, RoutedEventArgs e)
		{
			var currency = new Currency
			{
				Type = (CurrencyTypes)SourceCurrencyType.GetSelectedValue<CurrencyTypes>(),
				Value = Amount.Text.To<decimal>(),
			};

			Result.Content = currency.Convert((CurrencyTypes)TargetCurrencyType.GetSelectedValue<CurrencyTypes>()).Value;
		}
	}
}