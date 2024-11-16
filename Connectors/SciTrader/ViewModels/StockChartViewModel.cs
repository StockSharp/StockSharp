using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using DevExpress.Mvvm.POCO;
using SciTrader.DataSources;
using DevExpress.Xpf.Charts;
using SciTrader.Data;

namespace SciTrader.ViewModels {
    public class StockChartViewModel {
        const int initialVisiblePointsCount = 180;
        const int maxVisiblePointsCount = 800;

        public static StockChartViewModel Create() {
            return ViewModelSource.Create(() => new StockChartViewModel());
        }

        readonly StockDataSource dataSource;

        SymbolData symbol;
        bool initRange = false;

        public ObservableCollection<TradingData> ChartDataSource { get { return dataSource.Data; } }

        public virtual object MinVisibleDate { get; set; }
        public virtual ChartIntervalItem SelectedInterval { get; set; }
        public List<ChartIntervalItem> IntervalsSource { get; private set; }
        public virtual string CrosshairCurrentFinancialText { get; set; }
        public virtual string CrosshairCurrentVolumeText { get; set; }
        public virtual string SymbolName { get; set; }
        public virtual double CurrentPrice { get; set; }
        public virtual Color PriceIndicatorColor{ get; set; }
        public virtual bool AnnotationEditing { get; set; }

        public StockChartViewModel() {
            dataSource = new StockDataSource();
            IntervalsSource = new List<ChartIntervalItem>();
            InitIntervals();
            SelectedInterval = IntervalsSource[4];
        }
        void InitIntervals() {
            IntervalsSource.Add(new ChartIntervalItem() { Caption = "5 minutes", MeasureUnit = DateTimeMeasureUnit.Minute, MeasureUnitMultiplier = 5 });
            IntervalsSource.Add(new ChartIntervalItem() { Caption = "15 minutes", MeasureUnit = DateTimeMeasureUnit.Minute, MeasureUnitMultiplier = 15 });
            IntervalsSource.Add(new ChartIntervalItem() { Caption = "30 minutes", MeasureUnit = DateTimeMeasureUnit.Minute, MeasureUnitMultiplier = 30 });
            IntervalsSource.Add(new ChartIntervalItem() { Caption = "45 minutes", MeasureUnit = DateTimeMeasureUnit.Minute, MeasureUnitMultiplier = 45 });
            IntervalsSource.Add(new ChartIntervalItem() { Caption = "1 hour", MeasureUnit = DateTimeMeasureUnit.Hour, MeasureUnitMultiplier = 1 });
            IntervalsSource.Add(new ChartIntervalItem() { Caption = "2 hour", MeasureUnit = DateTimeMeasureUnit.Hour, MeasureUnitMultiplier = 2 });
            IntervalsSource.Add(new ChartIntervalItem() { Caption = "4 hour", MeasureUnit = DateTimeMeasureUnit.Hour, MeasureUnitMultiplier = 4 });
            IntervalsSource.Add(new ChartIntervalItem() { Caption = "6 hour", MeasureUnit = DateTimeMeasureUnit.Hour, MeasureUnitMultiplier = 6 });
            IntervalsSource.Add(new ChartIntervalItem() { Caption = "12 hour", MeasureUnit = DateTimeMeasureUnit.Hour, MeasureUnitMultiplier = 12 });
            IntervalsSource.Add(new ChartIntervalItem() { Caption = "1 day", MeasureUnit = DateTimeMeasureUnit.Day, MeasureUnitMultiplier = 1 });
            IntervalsSource.Add(new ChartIntervalItem() { Caption = "1 week", MeasureUnit = DateTimeMeasureUnit.Week, MeasureUnitMultiplier = 1 });
            IntervalsSource.Add(new ChartIntervalItem() { Caption = "1 month", MeasureUnit = DateTimeMeasureUnit.Month, MeasureUnitMultiplier = 1 });
        }
        void AppendChartData() {
            dataSource.AppendHistoricalData(SelectedInterval);
        }
        void GenerateInitialData() {
            if (symbol != null)
                dataSource.Init(SelectedInterval, symbol.BasePrice);
        }        
        void InitChartRange(ChartControl chart) {
            if (!initRange) {
                ((XYDiagram2D)chart.Diagram).ActualAxisX.ActualVisualRange.SetAuto();
                MinVisibleDate =  DateTime.Now - DateTimeHelper.ConvertInterval(SelectedInterval, initialVisiblePointsCount);
                initRange = true;
            }
        }
        void ReinitChartRange() {
            initRange = false;
        }
        void UpdateCrosshairText() {
            TradingData lastPoint = dataSource.Data.Last();
            CrosshairCurrentFinancialText = string.Format("O{0:f2}\tH{1:f2}\tL{2:f2}\tC{3:f2}\t", lastPoint.Open, lastPoint.High, lastPoint.Low, lastPoint.Close);
            CrosshairCurrentVolumeText = string.Format("{0:f2}", lastPoint.Volume);
        }
        protected void OnSelectedIntervalChanged() {
            ReinitChartRange();
            GenerateInitialData();
        }
        protected void OnAnnotationEditingChanged() {
            if (AnnotationEditing)
                dataSource.SuspendUpdate();
            else
                dataSource.ResumeUpdate();
        }

        public void DataChanged(RoutedEventArgs e) {
            ChartControl chart = e.Source as ChartControl;
            if (chart != null)
                InitChartRange(chart);
        }
        public void ChartScroll(XYDiagram2DScrollEventArgs eventArgs) {
            if(eventArgs.AxisX != null) {
                if ((DateTime)eventArgs.AxisX.ActualVisualRange.ActualMinValue < (DateTime)eventArgs.AxisX.ActualWholeRange.ActualMinValue)
                    AppendChartData();
            }
        }
        public void ChartZoom(XYDiagram2DZoomEventArgs eventArgs) {
            ManualDateTimeScaleOptions scaleOptions = eventArgs.AxisX.DateTimeScaleOptions as ManualDateTimeScaleOptions;
            if(scaleOptions != null) {
                TimeSpan measureUnitInterval = DateTimeHelper.GetInterval(scaleOptions.MeasureUnit, scaleOptions.MeasureUnitMultiplier);
                DateTime max = (DateTime)eventArgs.AxisX.ActualVisualRange.ActualMaxValue;
                DateTime min = (DateTime)eventArgs.AxisX.ActualVisualRange.ActualMinValue;
                TimeSpan duration = max - min;
                double visibleUnitsCount = duration.TotalSeconds / measureUnitInterval.TotalSeconds;
                if (visibleUnitsCount > maxVisiblePointsCount)
                    eventArgs.AxisX.VisualRange.SetMinMaxValues(eventArgs.OldXRange.MinValue, eventArgs.OldXRange.MaxValue);                
            }
        }
        public void UpdateData(ClosedTransactionData lastTransaction) {
            dataSource.UpdateLastPoint(lastTransaction.Price, lastTransaction.Volume);            
            UpdateCrosshairText();
            if (!AnnotationEditing) {
                CurrentPrice = lastTransaction.Price;
                PriceIndicatorColor = dataSource.Data.Last().VolumeColor;
            }
        }
        public void Init(SymbolData newSymbol) {
            symbol = newSymbol;
            GenerateInitialData();
            ReinitChartRange();
            SymbolName = symbol.Symbol;
            CurrentPrice = dataSource.Data.Last().Close;
        }
        public void CustomDrawCrosshair(CustomDrawCrosshairEventArgs e) {
            foreach (CrosshairLegendElement legendElement in e.CrosshairLegendElements) {
                Color color = ((TradingData)legendElement.SeriesPoint.Tag).VolumeColor;
                color.A = 255;
                legendElement.Foreground = new SolidColorBrush(color);
            }
        }
        public void AnnotationsChanged(NotifyCollectionChangedEventArgs e) {
            if (e.NewItems != null && e.NewItems.Count > 0) {
                foreach (Annotation annotation in e.NewItems) {
                    if (annotation.Content is EditableTextContent) {
                        EditableTextContent textContent = annotation.Content as EditableTextContent;
                        textContent.SetBinding(EditableTextContent.EditModeEnabledProperty, new Binding("AnnotationEditing") { Mode = BindingMode.OneWayToSource });

                    }
                }
            }
        }
    }
}
