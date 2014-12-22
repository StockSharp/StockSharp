namespace SampleDdeCustomTable
{
	using Ecng.Collections;
	using Ecng.Xaml;

	public partial class CandlesWindow
	{
		public CandlesWindow()
		{
			InitializeComponent();

			var candlesSource = new ObservableCollectionEx<QuikCandle>();
			CandleDetails.ItemsSource = candlesSource;
			Candles = new ThreadSafeObservableCollection<QuikCandle>(candlesSource);
		}

		public IListEx<QuikCandle> Candles { get; private set; }
	}
}