// *************************************************************************************
// SCICHART® Copyright SciChart Ltd. 2011-2023. All rights reserved.
//  
// Web: http://www.scichart.com
//   Support: support@scichart.com
//   Sales:   sales@scichart.com
// 
// PricePaneViewModel.cs is part of the SCICHART® Examples. Permission is hereby granted
// to modify, create derivative works, distribute and publish any part of this source
// code whether for commercial, private or personal use. 
// 
// The SCICHART® examples are distributed in the hope that they will be useful, but
// without any warranty. It is provided "AS IS" without warranty of any kind, either
// expressed or implied. 
// *************************************************************************************
using System;
using System.Diagnostics;
using System.Linq;
using SciChart.Charting.Model.ChartSeries;
using SciChart.Charting.Model.DataSeries;
using SciChart.Data.Model;
using SciChart.Examples.ExternalDependencies.Data;

namespace SciTrader.ViewModels
{
    public class PricePaneViewModel : BaseChartPaneViewModel
    {
        private readonly object _tickLocker = new object();
        private OhlcDataSeries<DateTime, double> _stockPrices;
        private XyDataSeries<DateTime, double> _maLow;
        private XyDataSeries<DateTime, double> _maHigh;

        private PriceBar _lastPrice;

        public PricePaneViewModel(CreateMultiPaneStockChartsViewModel parentViewModel/*, PriceSeries prices*/)
            : base(parentViewModel)
        {
            // We can add Series via the SeriesSource API, where SciStockChart or SciChartSurface bind to IEnumerable<IChartSeriesViewModel>
            // Alternatively, you can delcare your RenderableSeries in the SciStockChart and just bind to DataSeries
            // A third method (which we don't have an example for yet, but you can try out) is to create an Attached Behaviour to transform a collection of IDataSeries into IRenderableSeries

            // Add the main OHLC chart
            _stockPrices = new OhlcDataSeries<DateTime, double>() { SeriesName = "OHLC" };
            _stockPrices.FifoCapacity = 1500;
            //stockPrices.Append(prices.TimeData, prices.OpenData, prices.HighData, prices.LowData, prices.CloseData);
            ChartSeriesViewModels.Add(new CandlestickRenderableSeriesViewModel
            {
                
                DataSeries = _stockPrices,
                AntiAliasing = false,
                
            });

            // Add a moving average
            _maLow = new XyDataSeries<DateTime, double>() { SeriesName = "Low Line" };
            //maLow.Append(prices.TimeData, prices.CloseData.MovingAverage(50));
            ChartSeriesViewModels.Add(new LineRenderableSeriesViewModel
            {
                DataSeries = _maLow,
                StyleKey = "LowLineStyle",
            });

            // Add a moving average
            _maHigh = new XyDataSeries<DateTime, double>() { SeriesName = "High Line" };
            //maHigh.Append(prices.TimeData, prices.CloseData.MovingAverage(200));
            ChartSeriesViewModels.Add(new LineRenderableSeriesViewModel
            {
                DataSeries = _maHigh,
                StyleKey = "HighLineStyle",
            });

            YAxisTextFormatting = "$0.0000";
        }

        public override int GetDataCount()
        {
            return _stockPrices.Count;
        }

        public override void UpdatePriceSeries(PriceSeries priceSeries)
        {
            _stockPrices.Clear();
            _maLow.Clear();
            _maHigh.Clear();
            _stockPrices.Append(priceSeries.TimeData, priceSeries.OpenData, priceSeries.HighData, priceSeries.LowData, priceSeries.CloseData);
            _maLow.Append(priceSeries.TimeData, priceSeries.CloseData.MovingAverage(50));
            _maHigh.Append(priceSeries.TimeData, priceSeries.CloseData.MovingAverage(200));
            _lastPrice = priceSeries.Last();
        }

        public override void UpdateRealTimeData(PriceBar price)
        {
            // Ensure only one update processed at a time from multi-threaded timer
            lock (_tickLocker)
            {
                // Update the last price, or append? 
                var ds0 = (IOhlcDataSeries<DateTime, double>)_stockPrices;
                //var ds1 = (IXyDataSeries<DateTime, double>)_maLow;
                if (_lastPrice != null && _lastPrice.DateTime >= price.DateTime)
                {
                    ds0.Update(price.DateTime, price.Open, price.High, price.Low, price.Close);
                }
                else
                {
                    ds0.Append(price.DateTime, price.Open, price.High, price.Low, price.Close);
                }

                _lastPrice = price;
            }
        }
    }
}
