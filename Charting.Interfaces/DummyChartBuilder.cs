namespace StockSharp.Charting;

using System;
using System.ComponentModel;
using System.Drawing;

using Ecng.Collections;
using Ecng.ComponentModel;
using Ecng.Serialization;

/// <summary>
/// Dummy implementation of <see cref="IChartBuilder"/>.
/// </summary>
public class DummyChartBuilder : IChartBuilder
{
	private class DummyPersistable : IPersistable, INotifyPropertyChanged, INotifyPropertyChanging
	{
		event PropertyChangingEventHandler INotifyPropertyChanging.PropertyChanging
		{
			add { }
			remove { }
		}

		event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
		{
			add { }
			remove { }
		}

		void IPersistable.Load(SettingsStorage storage) { }
		void IPersistable.Save(SettingsStorage storage) { }
	}

	private class DummyPart<T> : DummyPersistable, IChartPart<T>
		where T : IChartPart<T>
	{
		Guid IChartPart<T>.Id { get; } = Guid.NewGuid();
	}

	private class DummyElement : DummyPart<IChartElement>, IChartElement
	{
		string IChartElement.FullTitle { get; set; }
		bool IChartElement.IsVisible { get; set; }
		bool IChartElement.IsLegend { get; set; }
		string IChartElement.XAxisId { get; set; }
		string IChartElement.YAxisId { get; set; }
		Func<IComparable, Color?> IChartElement.Colorer { get; set; }

		IChartAxis IChartElement.XAxis => throw new NotImplementedException();
		IChartAxis IChartElement.YAxis => throw new NotImplementedException();

		IChart IChartElement.Chart => throw new NotImplementedException();
		IChartArea IChartElement.ChartArea => throw new NotImplementedException();
		IChartArea IChartElement.PersistantChartArea => throw new NotImplementedException();
	}

	private class DummyActiveOrdersElement : DummyElement, IChartActiveOrdersElement
	{
		Color IChartActiveOrdersElement.BuyPendingColor { get; set; }
		Color IChartActiveOrdersElement.BuyColor { get; set; }
		Color IChartActiveOrdersElement.BuyBlinkColor { get; set; }
		Color IChartActiveOrdersElement.SellPendingColor { get; set; }
		Color IChartActiveOrdersElement.SellColor { get; set; }
		Color IChartActiveOrdersElement.SellBlinkColor { get; set; }
		Color IChartActiveOrdersElement.CancelButtonColor { get; set; }
		Color IChartActiveOrdersElement.CancelButtonBackground { get; set; }
		Color IChartActiveOrdersElement.ForegroundColor { get; set; }
		bool IChartActiveOrdersElement.IsAnimationEnabled { get; set; }
	}

	private class DummyAnnotation : DummyElement, IChartAnnotation
	{
		ChartAnnotationTypes IChartAnnotation.Type { get; set; }
	}

	private class DummyArea : DummyPart<IChartArea>, IChartArea
	{
		bool IChartArea.IsAutoRange { get; set; }

		INotifyList<IChartElement> IChartArea.Elements { get; } = new SynchronizedList<IChartElement>();
		INotifyList<IChartAxis> IChartArea.XAxises { get; } = new SynchronizedList<IChartAxis>();
		INotifyList<IChartAxis> IChartArea.YAxises { get; } = new SynchronizedList<IChartAxis>();

		ChartAxisType IChartArea.XAxisType { get; set; }
		string IChartArea.Title { get; set; }
		double IChartArea.Height { get; set; }
		IChart IChartArea.Chart => throw new NotImplementedException();
	}

	private class DummyAxis : DummyPersistable, IChartAxis
	{
		IChartArea IChartAxis.ChartArea => throw new NotImplementedException();

		string IChartAxis.Id { get; set; }
		bool IChartAxis.IsVisible { get; set; }
		string IChartAxis.Title { get; set; }
		string IChartAxis.Group { get; set; }
		bool IChartAxis.SwitchAxisLocation { get; set; }
		ChartAxisType IChartAxis.AxisType { get; set; }
		bool IChartAxis.AutoRange { get; set; }
		bool IChartAxis.FlipCoordinates { get; set; }
		bool IChartAxis.DrawMajorTicks { get; set; }
		bool IChartAxis.DrawMajorGridLines { get; set; }
		bool IChartAxis.DrawMinorTicks { get; set; }
		bool IChartAxis.DrawMinorGridLines { get; set; }
		bool IChartAxis.DrawLabels { get; set; }
		string IChartAxis.TextFormatting { get; set; }
		string IChartAxis.SubDayTextFormatting { get; set; }
		TimeZoneInfo IChartAxis.TimeZone { get; set; }

		void INotifyPropertyChangedEx.NotifyPropertyChanged(string propertyName)
			=> throw new NotImplementedException();
	}

	private class DummyBandElement : DummyElement, IChartBandElement
	{
		ChartIndicatorDrawStyles IChartBandElement.Style { get; set; }
		IChartLineElement IChartBandElement.Line1 => throw new NotImplementedException();
		IChartLineElement IChartBandElement.Line2 => throw new NotImplementedException();
	}

	private class DummyBubbleElement : DummyElement, IChartLineElement
	{
		Color IChartLineElement.Color { get; set; }
		Color IChartLineElement.AdditionalColor { get; set; }
		int IChartLineElement.StrokeThickness { get; set; }
		bool IChartLineElement.AntiAliasing { get; set; }
		ChartIndicatorDrawStyles IChartLineElement.Style { get; set; }
		bool IChartLineElement.ShowAxisMarker { get; set; }
	}

	private class DummyLineElement : DummyElement, IChartLineElement
	{
		Color IChartLineElement.Color { get; set; }
		Color IChartLineElement.AdditionalColor { get; set; }
		int IChartLineElement.StrokeThickness { get; set; }
		bool IChartLineElement.AntiAliasing { get; set; }
		ChartIndicatorDrawStyles IChartLineElement.Style { get; set; }
		bool IChartLineElement.ShowAxisMarker { get; set; }
	}

	private class DummyCandleElement : DummyElement, IChartCandleElement
	{
		ChartCandleDrawStyles IChartCandleElement.DrawStyle { get; set; }
		Color IChartCandleElement.DownFillColor { get; set; }
		Color IChartCandleElement.UpFillColor { get; set; }
		Color IChartCandleElement.DownBorderColor { get; set; }
		Color IChartCandleElement.UpBorderColor { get; set; }
		int IChartCandleElement.StrokeThickness { get; set; }
		bool IChartCandleElement.AntiAliasing { get; set; }
		Color IChartCandleElement.LineColor { get; set; }
		Color IChartCandleElement.AreaColor { get; set; }
		bool IChartCandleElement.ShowAxisMarker { get; set; }
		Func<DateTimeOffset, bool, bool, Color?> IChartCandleElement.Colorer { get; set; }
		int IChartCandleElement.Timeframe2Multiplier { get; set; }
		int IChartCandleElement.Timeframe3Multiplier { get; set; }
		Color IChartCandleElement.FontColor { get; set; }
		Color IChartCandleElement.Timeframe2Color { get; set; }
		Color IChartCandleElement.Timeframe2FrameColor { get; set; }
		Color IChartCandleElement.Timeframe3Color { get; set; }
		Color IChartCandleElement.MaxVolumeColor { get; set; }
		Color IChartCandleElement.ClusterLineColor { get; set; }
		Color IChartCandleElement.ClusterSeparatorLineColor { get; set; }
		Color IChartCandleElement.ClusterTextColor { get; set; }
		Color IChartCandleElement.ClusterColor { get; set; }
		Color IChartCandleElement.ClusterMaxColor { get; set; }
		bool IChartCandleElement.ShowHorizontalVolumes { get; set; }
		bool IChartCandleElement.LocalHorizontalVolumes { get; set; }
		double IChartCandleElement.HorizontalVolumeWidthFraction { get; set; }
		Color IChartCandleElement.HorizontalVolumeColor { get; set; }
		Color IChartCandleElement.HorizontalVolumeFontColor { get; set; }
	}

	private class DummyTransactionElement : DummyElement, IChartTransactionElement
	{
		Color IChartTransactionElement.BuyColor { get; set; }
		Color IChartTransactionElement.BuyStrokeColor { get; set; }
		Color IChartTransactionElement.SellColor { get; set; }
		Color IChartTransactionElement.SellStrokeColor { get; set; }
		bool IChartTransactionElement.UseAltIcon { get; set; }
		double IChartTransactionElement.DrawSize { get; set; }
	}

	private class DummyOrderElement : DummyTransactionElement, IChartOrderElement
	{
		Color IChartOrderElement.ErrorColor { get; set; }
		Color IChartOrderElement.ErrorStrokeColor { get; set; }
		ChartOrderDisplayFilter IChartOrderElement.Filter { get; set; }
	}

	private class DummyTradeElement : DummyTransactionElement, IChartTradeElement
	{
	}

	private class DummyIndicatorElement : DummyElement, IChartIndicatorElement
	{
		IChartIndicatorPainter IChartIndicatorElement.IndicatorPainter { get; set; }
		Color IChartIndicatorElement.Color { get; set; }
		Color IChartIndicatorElement.AdditionalColor { get; set; }
		int IChartIndicatorElement.StrokeThickness { get; set; }
		bool IChartIndicatorElement.AntiAliasing { get; set; }
		ChartIndicatorDrawStyles IChartIndicatorElement.DrawStyle { get; set; }
		bool IChartIndicatorElement.ShowAxisMarker { get; set; }
		bool IChartIndicatorElement.AutoAssignYAxis { get; set; }
	}

	IChartActiveOrdersElement IChartBuilder.CreateActiveOrdersElement() => new DummyActiveOrdersElement();
	IChartAnnotation IChartBuilder.CreateAnnotation() => new DummyAnnotation();
	IChartArea IChartBuilder.CreateArea() => new DummyArea();
	IChartAxis IChartBuilder.CreateAxis() => new DummyAxis();
	IChartBandElement IChartBuilder.CreateBandElement() => new DummyBandElement();
	IChartLineElement IChartBuilder.CreateBubbleElement() => new DummyBubbleElement();
	IChartCandleElement IChartBuilder.CreateCandleElement() => new DummyCandleElement();
	IChartIndicatorElement IChartBuilder.CreateIndicatorElement() => new DummyIndicatorElement();
	IChartLineElement IChartBuilder.CreateLineElement() => new DummyLineElement();
	IChartOrderElement IChartBuilder.CreateOrderElement() => new DummyOrderElement();
	IChartTradeElement IChartBuilder.CreateTradeElement() => new DummyTradeElement();
}