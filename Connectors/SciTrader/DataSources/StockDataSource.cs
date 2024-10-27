using SciTrader.Data;
using DevExpress.Xpf.Charts;
using DevExpress.Xpf.Core;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using DevExpress.Data.Utils;

namespace SciTrader.DataSources {
    public class StockDataSource {
        const int pointsLimit = 1200;
        const int yearLimit = 1991;
        const int volumeMinLow = 300000;
        const int volumeMaxLow = 600000;
        const int volumeMinHigh = 1000000;
        const int volumeMaxHigh = 2500000;

        readonly IntervalTimer intervalTimer;
        double basePrice;

        readonly ObservableCollectionCore<TradingData> data = new ObservableCollectionCore<TradingData>();

        public ObservableCollection<TradingData> Data { get { return data; } }

        public StockDataSource() {
            intervalTimer = new IntervalTimer();
            intervalTimer.OnTickChanged += OnTickChanged;
        }

        void OnTickChanged(object sender, EventArgs e) {
            AddNewLastPoint();
        }

        void AddNewLastPoint() {
            DateTime timeStamp = DateTime.Now;
            TradingData lastData = data.Last();
            double value = lastData.Close;
            TradingData newData = new TradingData(timeStamp, value, value, value, value, 0);
            data.Add(newData);
            if (lastData.UpdateSuspended) {
                lastData.ResumeUpdate();
                newData.SuspendUpdate();
            }
        }

        DateTime GetPreviousPointDate(DateTime pointDate, ChartIntervalItem interval) {
            DateTime newDate;
            switch (interval.MeasureUnit) {
                case DateTimeMeasureUnit.Second:
                    newDate = pointDate.AddSeconds(-interval.MeasureUnitMultiplier);
                    if (newDate.DayOfWeek == DayOfWeek.Saturday)
                        newDate = newDate.Date.AddSeconds(-interval.MeasureUnitMultiplier);
                    if (newDate.DayOfWeek == DayOfWeek.Sunday)
                        newDate = newDate.Date.AddDays(-1).AddSeconds(-interval.MeasureUnitMultiplier);
                    return newDate;
                case DateTimeMeasureUnit.Minute:
                    newDate = pointDate.AddMinutes(-interval.MeasureUnitMultiplier);
                    if (newDate.DayOfWeek == DayOfWeek.Saturday)
                        newDate = newDate.Date.AddMinutes(-interval.MeasureUnitMultiplier);
                    if (newDate.DayOfWeek == DayOfWeek.Sunday)
                        newDate = newDate.Date.AddDays(-1).AddMinutes(-interval.MeasureUnitMultiplier);
                    return newDate;
                case DateTimeMeasureUnit.Hour:
                    newDate = pointDate.AddHours(-interval.MeasureUnitMultiplier);
                    if (newDate.DayOfWeek == DayOfWeek.Saturday)
                        newDate = newDate.Date.AddHours(-interval.MeasureUnitMultiplier);
                    if (newDate.DayOfWeek == DayOfWeek.Sunday)
                        newDate = newDate.Date.AddDays(-1).AddHours(-interval.MeasureUnitMultiplier);
                    return newDate;
                case DateTimeMeasureUnit.Day:
                    newDate = pointDate.AddDays(-interval.MeasureUnitMultiplier);
                    if (newDate.DayOfWeek == DayOfWeek.Saturday)
                        newDate = newDate.Date.AddDays(-interval.MeasureUnitMultiplier);
                    if (newDate.DayOfWeek == DayOfWeek.Sunday)
                        newDate = newDate.Date.AddDays(-(interval.MeasureUnitMultiplier + 1));
                    return newDate;
                case DateTimeMeasureUnit.Week:
                    newDate = pointDate.AddDays(-interval.MeasureUnitMultiplier * 7);
                    return newDate;
                case DateTimeMeasureUnit.Month:
                    newDate = pointDate.AddMonths(-interval.MeasureUnitMultiplier);
                    newDate = new DateTime(newDate.Year, newDate.Month, 1);
                    if (newDate.DayOfWeek == DayOfWeek.Saturday)
                        newDate = newDate.Date.AddDays(2);
                    if (newDate.DayOfWeek == DayOfWeek.Sunday)
                        newDate = newDate.Date.AddDays(1);
                    return newDate;
            }
            return pointDate;
        }

        void InsertPoints(ChartIntervalItem interval, double currentPrice, DateTime date) {
            double open;
            double close = currentPrice;
            double low;
            double high;
            double basePriceDeviation = basePrice * MarketDataProvider.PriceDeviation;
            NonCryptographicRandom rnd = NonCryptographicRandom.Default;
            int direction = 0;
            double directionMultiplier = 1;
            int volumeMin = volumeMinLow;
            int volumeMax = volumeMaxLow;
            data.BeginUpdate();
            for (int i = 0; i < pointsLimit; i++) {
                if (direction == 0) {
                    direction = rnd.Next(1, 5);
                    directionMultiplier *= -1;

                    if(rnd.NextDouble() > 0.5) {
                        volumeMin = volumeMinHigh;
                        volumeMax = volumeMaxHigh;
                    }
                    else {
                        volumeMin = volumeMinLow;
                        volumeMax = volumeMaxLow;
                    }
                }
                open = close + directionMultiplier * rnd.NextDouble() * basePriceDeviation;
                if(open < basePrice * 0.5) {
                    double correction = rnd.NextDouble() * basePriceDeviation;
                    open += correction;
                    close += correction;
                }
                high = Math.Max(open, close) + rnd.NextDouble() * basePriceDeviation;
                low = Math.Min(open, close) - rnd.NextDouble() * basePriceDeviation;
                double volume = rnd.Next(volumeMin, volumeMax);
                TradingData pointData = new TradingData(date, open, high, low, close, volume);
                data.Insert(0, pointData);
                date = GetPreviousPointDate(date, interval);
                if (date.Year < yearLimit)
                    break;
                close = open;
                direction--;
            }
            data.EndUpdate();
        }

        public void Init(ChartIntervalItem interval, double basePrice) {
            this.basePrice = basePrice;
            data.Clear();
            DateTime date = DateTimeHelper.GetInitialDate(interval);
            InsertPoints(interval, basePrice, date);
            intervalTimer.SetInterval(interval);
        }
        public void AppendHistoricalData(ChartIntervalItem interval) {
            DateTime date = GetPreviousPointDate(data[0].Date, interval);
            if (date.Year >= yearLimit)
                InsertPoints(interval, data[0].Low, data[0].Date);
        }
        public void UpdateLastPoint(double price, double volumeChange) {
            TradingData lastPoint = data.Last();
            double high = lastPoint.High;
            if (price > high)
                high = price;
            double low = lastPoint.Low;
            if (price < low)
                low = price;
            lastPoint.High = high;
            lastPoint.Low = low;
            lastPoint.Close = price;
            lastPoint.Volume += volumeChange;
        }

        public void SuspendUpdate() {
            data.Last().SuspendUpdate();
        }
        public void ResumeUpdate() {
            data.Last().ResumeUpdate();
        }
    }
}
