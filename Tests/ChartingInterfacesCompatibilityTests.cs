namespace StockSharp.Tests;

using System.ComponentModel;
using System.Drawing;

using StockSharp.Charting;

using static StockSharp.Charting.IChartDrawData;

[TestClass]
public class ChartingInterfacesCompatibilityTests : BaseTestClass
{
	private class OldDrawDataItem : IChartDrawDataItem
	{
		public int CandleCount { get; private set; }
		public decimal ClosePrice { get; private set; }

		public IChartDrawDataItem Add(IChartCandleElement element, Color? color)
			=> this;

		public IChartDrawDataItem Add(IChartCandleElement element, DataType dataType, SecurityId secId, decimal openPrice, decimal highPrice, decimal lowPrice, decimal closePrice, CandlePriceLevel[] priceLevels, CandleStates state)
		{
			CandleCount++;
			ClosePrice = closePrice;
			return this;
		}

		public IChartDrawDataItem Add(IChartIndicatorElement element, IIndicatorValue value)
			=> this;

		public IChartDrawDataItem Add(IChartOrderElement element, long orderId, string orderStringId, Sides side, decimal price, decimal volume, string errorMessage = null)
			=> this;

		public IChartDrawDataItem Add(IChartTradeElement element, long tradeId, string tradeStringId, Sides side, decimal price, decimal volume)
			=> this;

		public IChartDrawDataItem Add(IChartLineElement element, double value1, double value2 = double.NaN)
			=> this;

		public IChartDrawDataItem Add(IChartBandElement element, decimal value)
			=> this;

		public IChartDrawDataItem Add(IChartBandElement element, double value1, double value2)
			=> this;
	}

	private sealed class FullCandleDrawDataItem : OldDrawDataItem, IChartCandleDrawDataItem
	{
		public ChartCandleDrawData Candle { get; private set; }

		public IChartDrawDataItem Add(IChartCandleElement element, ChartCandleDrawData data)
		{
			Candle = data;
			return this;
		}
	}

	private class OldChartAxis : IChartAxis
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

		public IChartArea ChartArea => null;
		public string Id { get; set; }
		public bool IsVisible { get; set; }
		public string Title { get; set; }
		public string Group { get; set; }
		public bool SwitchAxisLocation { get; set; }
		public ChartAxisType AxisType { get; set; }
		public bool AutoRange { get; set; }
		public bool FlipCoordinates { get; set; }
		public bool DrawMajorTicks { get; set; }
		public bool DrawMajorGridLines { get; set; }
		public bool DrawMinorTicks { get; set; }
		public bool DrawMinorGridLines { get; set; }
		public bool DrawLabels { get; set; }
		public string TextFormatting { get; set; }
		public string CursorTextFormatting { get; set; }
		public string SubDayTextFormatting { get; set; }
		public TimeZoneInfo TimeZone { get; set; }

		public void Load(SettingsStorage storage)
		{
		}

		public void Save(SettingsStorage storage)
		{
		}

		void INotifyPropertyChangedEx.NotifyPropertyChanged(string propertyName)
		{
		}
	}

	private sealed class ManualRangeChartAxis : OldChartAxis, IChartManualRangeAxis
	{
		public decimal? MinValue { get; set; }
		public decimal? MaxValue { get; set; }
	}

	[TestMethod]
	public void OldDrawDataItemReceivesCandleThroughFallback()
	{
		IChartDrawDataItem item = new OldDrawDataItem();
		var element = Mock.Of<IChartCandleElement>();
		var candle = CreateCandle();

		var result = item.Add(element, candle);
		var oldItem = (OldDrawDataItem)item;

		AreSame(item, result);
		AreEqual(1, oldItem.CandleCount);
		AreEqual(candle.ClosePrice, oldItem.ClosePrice);
	}

	[TestMethod]
	public void FullCandleCapabilityReceivesTotalVolume()
	{
		IChartDrawDataItem item = new FullCandleDrawDataItem();
		var element = Mock.Of<IChartCandleElement>();
		var candle = CreateCandle();

		var result = item.Add(element, candle);
		var fullItem = (FullCandleDrawDataItem)item;

		AreSame(item, result);
		AreEqual(candle.TotalVolume, fullItem.Candle.TotalVolume);
		AreEqual(candle.ClosePrice, fullItem.Candle.ClosePrice);
		AreEqual(0, fullItem.CandleCount);
	}

	[TestMethod]
	public void OldChartAxisDoesNotRequireManualRange()
	{
		IChartAxis axis = new OldChartAxis { AutoRange = false };

		axis.ValidateManualRange();
	}

	[TestMethod]
	public void ManualRangeCapabilityValidatesBounds()
	{
		IChartAxis axis = new ManualRangeChartAxis
		{
			AutoRange = false,
			MinValue = 10,
			MaxValue = 5,
		};

		Throws<InvalidOperationException>(axis.ValidateManualRange);
	}

	private static TimeFrameCandleMessage CreateCandle()
		=> new()
		{
			SecurityId = Helper.CreateSecurityId(),
			TypedArg = TimeSpan.FromMinutes(1),
			OpenPrice = 10,
			HighPrice = 15,
			LowPrice = 8,
			ClosePrice = 12,
			TotalVolume = 42,
			State = CandleStates.Finished,
		};
}
