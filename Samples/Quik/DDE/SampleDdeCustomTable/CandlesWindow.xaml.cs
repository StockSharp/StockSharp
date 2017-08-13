#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleDdeCustomTable.SampleDdeCustomTablePublic
File: CandlesWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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

		public IListEx<QuikCandle> Candles { get; }
	}
}