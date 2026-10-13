namespace StockSharp.Tests;

using System.ComponentModel;
using System.Drawing;

using StockSharp.Charting;
using StockSharp.Localization;

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

		public Task LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
			=> Task.CompletedTask;

		public Task SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
			=> Task.CompletedTask;

		void INotifyPropertyChangedEx.NotifyPropertyChanged(string propertyName)
		{
		}
	}

	private sealed class ManualRangeChartAxis : OldChartAxis, IChartManualRangeAxis
	{
		public decimal? MinValue { get; set; }
		public decimal? MaxValue { get; set; }
	}

	/// <summary>
	/// Records which typed overload the untyped element/value dispatcher picked, and with which arguments.
	/// </summary>
	private sealed class RecordingDrawDataItem : IChartCandleDrawDataItem
	{
		public const string CandleCall = "candle";
		public const string CandleColorCall = "candleColor";
		public const string CandleFallbackCall = "candleFallback";
		public const string IndicatorCall = "indicator";
		public const string OrderCall = "order";
		public const string TradeCall = "trade";
		public const string LineCall = "line";
		public const string BandDecimalCall = "bandDecimal";
		public const string BandDoubleCall = "bandDouble";

		public string Call { get; private set; }
		public IChartElement Element { get; private set; }
		public ChartCandleDrawData Candle { get; private set; }
		public IIndicatorValue IndicatorValue { get; private set; }
		public long Id { get; private set; }
		public string StringId { get; private set; }
		public Sides Side { get; private set; }
		public decimal Price { get; private set; }
		public decimal Volume { get; private set; }
		public string ErrorMessage { get; private set; }
		public double Value1 { get; private set; }
		public double Value2 { get; private set; }
		public decimal DecimalValue { get; private set; }

		/// <summary>
		/// The value a band was drawn at, whichever of the two band overloads carried it.
		/// </summary>
		public double BandValue => Call == BandDecimalCall ? (double)DecimalValue : Value1;

		public IChartDrawDataItem Add(IChartCandleElement element, Color? color)
		{
			Call = CandleColorCall;
			Element = element;
			return this;
		}

		public IChartDrawDataItem Add(IChartCandleElement element, ChartCandleDrawData data)
		{
			Call = CandleCall;
			Element = element;
			Candle = data;
			return this;
		}

		public IChartDrawDataItem Add(IChartCandleElement element, DataType dataType, SecurityId secId, decimal openPrice, decimal highPrice, decimal lowPrice, decimal closePrice, CandlePriceLevel[] priceLevels, CandleStates state)
		{
			Call = CandleFallbackCall;
			Element = element;
			Candle = new(dataType, secId, openPrice, highPrice, lowPrice, closePrice, default, priceLevels, state);
			return this;
		}

		public IChartDrawDataItem Add(IChartIndicatorElement element, IIndicatorValue value)
		{
			Call = IndicatorCall;
			Element = element;
			IndicatorValue = value;
			return this;
		}

		public IChartDrawDataItem Add(IChartOrderElement element, long orderId, string orderStringId, Sides side, decimal price, decimal volume, string errorMessage = null)
		{
			Call = OrderCall;
			Element = element;
			Id = orderId;
			StringId = orderStringId;
			Side = side;
			Price = price;
			Volume = volume;
			ErrorMessage = errorMessage;
			return this;
		}

		public IChartDrawDataItem Add(IChartTradeElement element, long tradeId, string tradeStringId, Sides side, decimal price, decimal volume)
		{
			Call = TradeCall;
			Element = element;
			Id = tradeId;
			StringId = tradeStringId;
			Side = side;
			Price = price;
			Volume = volume;
			return this;
		}

		public IChartDrawDataItem Add(IChartLineElement element, double value1, double value2 = double.NaN)
		{
			Call = LineCall;
			Element = element;
			Value1 = value1;
			Value2 = value2;
			return this;
		}

		public IChartDrawDataItem Add(IChartBandElement element, decimal value)
		{
			Call = BandDecimalCall;
			Element = element;
			DecimalValue = value;
			return this;
		}

		public IChartDrawDataItem Add(IChartBandElement element, double value1, double value2)
		{
			Call = BandDoubleCall;
			Element = element;
			Value1 = value1;
			Value2 = value2;
			return this;
		}
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

	[TestMethod]
	public void AddByElementTypeRejectsNullElement()
	{
		IChartDrawDataItem item = new RecordingDrawDataItem();

		AreEqual("element", Throws<ArgumentNullException>(() => item.Add((IChartElement)null, 1d)).ParamName);
	}

	[TestMethod]
	public void AddByElementTypeRejectsUnknownElement()
	{
		IChartDrawDataItem item = new RecordingDrawDataItem();
		IChartElement element = Mock.Of<IChartElement>();

		Throws<ArgumentException>(() => item.Add(element, 1d));
	}

	[TestMethod]
	public void AddByElementTypeRoutesCandleWithAllPricesIntact()
	{
		var recorder = new RecordingDrawDataItem();
		IChartDrawDataItem item = recorder;
		IChartElement element = Mock.Of<IChartCandleElement>();
		var candle = CreateCandle();

		var result = item.Add(element, candle);

		AreSame(item, result);
		AreEqual(RecordingDrawDataItem.CandleCall, recorder.Call);
		AreSame(element, recorder.Element);
		AreEqual(candle.OpenPrice, recorder.Candle.OpenPrice);
		AreEqual(candle.HighPrice, recorder.Candle.HighPrice);
		AreEqual(candle.LowPrice, recorder.Candle.LowPrice);
		AreEqual(candle.ClosePrice, recorder.Candle.ClosePrice);
		AreEqual(candle.TotalVolume, recorder.Candle.TotalVolume);
		AreEqual(candle.State, recorder.Candle.State);
		AreEqual(candle.SecurityId, recorder.Candle.SecurityId);
	}

	[TestMethod]
	public void AddByElementTypeDrawsNothingForNullCandle()
	{
		var recorder = new RecordingDrawDataItem();
		IChartDrawDataItem item = recorder;
		IChartElement element = Mock.Of<IChartCandleElement>();

		// A missing candle must not reach the element as a candle drawn at zero prices.
		var result = item.Add(element, (ICandleMessage)null);

		AreSame(item, result);
		IsNull(recorder.Call);
	}

	[TestMethod]
	public void AddByElementTypeRoutesIndicatorValueUnchanged()
	{
		var recorder = new RecordingDrawDataItem();
		IChartDrawDataItem item = recorder;
		IChartElement element = Mock.Of<IChartIndicatorElement>();
		var value = Mock.Of<IIndicatorValue>();

		item.Add(element, value);

		AreEqual(RecordingDrawDataItem.IndicatorCall, recorder.Call);
		AreSame(element, recorder.Element);
		AreSame(value, recorder.IndicatorValue);
	}

	[TestMethod]
	public void AddByElementTypeMarksFailedOrderWithFailedText()
	{
		var recorder = new RecordingDrawDataItem();
		IChartDrawDataItem item = recorder;
		IChartElement element = Mock.Of<IChartOrderElement>();
		var order = CreateOrder(OrderStates.Failed);

		item.Add(element, order);

		// A rejected order is the only reason the chart shows an error marker, and the order is keyed
		// by its transaction id because a failed order never received a board id.
		AreEqual(RecordingDrawDataItem.OrderCall, recorder.Call);
		AreEqual(LocalizedStrings.Failed, recorder.ErrorMessage);
		AreEqual(order.TransactionId, recorder.Id);
		IsNull(recorder.StringId);
		AreEqual(order.Side, recorder.Side);
		AreEqual(order.Price, recorder.Price);
		AreEqual(order.Volume, recorder.Volume);
	}

	[TestMethod]
	public void AddByElementTypeLeavesLiveOrderWithoutErrorText()
	{
		var recorder = new RecordingDrawDataItem();
		IChartDrawDataItem item = recorder;
		IChartElement element = Mock.Of<IChartOrderElement>();
		var order = CreateOrder(OrderStates.Active);

		item.Add(element, order);

		AreEqual(RecordingDrawDataItem.OrderCall, recorder.Call);
		IsNull(recorder.ErrorMessage);
	}

	[TestMethod]
	public void AddByElementTypeDoesNotDereferenceNullOrder()
	{
		var recorder = new RecordingDrawDataItem();
		IChartDrawDataItem item = recorder;
		IChartElement element = Mock.Of<IChartOrderElement>();

		// Every other branch answers a null value either by refusing it or by drawing nothing.
		// A NullReferenceException is neither, and is never something a caller can act on.
		Exception caught = null;

		try
		{
			item.Add(element, (Order)null);
		}
		catch (Exception e)
		{
			caught = e;
		}

		IsFalse(caught is NullReferenceException, $"Null order surfaced as {caught}.");
		IsNull(recorder.Call);
	}

	[TestMethod]
	public void AddByElementTypeTakesTradeIdsFromTickAndSideFromOrder()
	{
		var recorder = new RecordingDrawDataItem();
		IChartDrawDataItem item = recorder;
		IChartElement element = Mock.Of<IChartTradeElement>();

		var order = CreateOrder(OrderStates.Done);
		order.Side = Sides.Sell;

		var trade = new MyTrade
		{
			Order = order,
			Trade = new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradeId = 777,
				TradeStringId = "T-777",
				TradePrice = 101.5m,
				TradeVolume = 3m,
			},
		};

		item.Add(element, trade);

		// The tick has no own side on the chart: a fill is drawn on the side of the order that made it.
		AreEqual(RecordingDrawDataItem.TradeCall, recorder.Call);
		AreEqual(777L, recorder.Id);
		AreEqual("T-777", recorder.StringId);
		AreEqual(Sides.Sell, recorder.Side);
		AreEqual(101.5m, recorder.Price);
		AreEqual(3m, recorder.Volume);
	}

	[TestMethod]
	public void AddByElementTypeDrawsLineDoubleWithSecondCoordinateUnset()
	{
		var recorder = new RecordingDrawDataItem();
		IChartDrawDataItem item = recorder;
		IChartElement element = Mock.Of<IChartLineElement>();

		item.Add(element, 3.5d);

		// One value means one point: the second coordinate stays NaN, the interface default for "not given".
		AreEqual(RecordingDrawDataItem.LineCall, recorder.Call);
		AreEqual(3.5d, recorder.Value1);
		IsTrue(double.IsNaN(recorder.Value2));
	}

	[TestMethod]
	public void AddByElementTypeConvertsLineDecimalToDouble()
	{
		var recorder = new RecordingDrawDataItem();
		IChartDrawDataItem item = recorder;
		IChartElement element = Mock.Of<IChartLineElement>();

		// 12.25 is exact in binary floating point, so the decimal and the double name the same number.
		item.Add(element, 12.25m);

		AreEqual(RecordingDrawDataItem.LineCall, recorder.Call);
		AreEqual(12.25d, recorder.Value1);
		IsTrue(double.IsNaN(recorder.Value2));
	}

	[TestMethod]
	public void AddByElementTypeDrawsLineTupleAsBothCoordinates()
	{
		var recorder = new RecordingDrawDataItem();
		IChartDrawDataItem item = recorder;
		IChartElement element = Mock.Of<IChartLineElement>();

		item.Add(element, (1.5d, 2.5d));

		AreEqual(RecordingDrawDataItem.LineCall, recorder.Call);
		AreEqual(1.5d, recorder.Value1);
		AreEqual(2.5d, recorder.Value2);
	}

	[TestMethod]
	public void AddByElementTypeDrawsBandDecimalAtTheSameValue()
	{
		var recorder = new RecordingDrawDataItem();
		IChartDrawDataItem item = recorder;
		IChartElement element = Mock.Of<IChartBandElement>();

		item.Add(element, 7.25m);

		AreEqual(RecordingDrawDataItem.BandDecimalCall, recorder.Call);
		AreEqual(7.25m, recorder.DecimalValue);
	}

	[TestMethod]
	public void AddByElementTypeDrawsBandDoubleAtTheSameValue()
	{
		var recorder = new RecordingDrawDataItem();
		IChartDrawDataItem item = recorder;
		IChartElement element = Mock.Of<IChartBandElement>();

		// 7.25 is exact in binary floating point, so a double and a decimal 7.25 are the same number and
		// must reach the band as the same value whichever band overload the dispatcher chooses.
		item.Add(element, 7.25d);

		AreEqual(7.25d, recorder.BandValue);
	}

	[TestMethod]
	public void AddByElementTypeDrawsBandTupleAsBothCoordinates()
	{
		var recorder = new RecordingDrawDataItem();
		IChartDrawDataItem item = recorder;
		IChartElement element = Mock.Of<IChartBandElement>();

		item.Add(element, (1.5d, 2.5d));

		AreEqual(RecordingDrawDataItem.BandDoubleCall, recorder.Call);
		AreEqual(1.5d, recorder.Value1);
		AreEqual(2.5d, recorder.Value2);
	}

	[TestMethod]
	public void AddByElementTypeRejectsNullLineAndBandValues()
	{
		IChartDrawDataItem item = new RecordingDrawDataItem();
		IChartElement line = Mock.Of<IChartLineElement>();
		IChartElement band = Mock.Of<IChartBandElement>();

		AreEqual("value", Throws<ArgumentNullException>(() => item.Add(line, null)).ParamName);
		AreEqual("value", Throws<ArgumentNullException>(() => item.Add(band, null)).ParamName);
	}

	[TestMethod]
	public void AddByElementTypeRejectsUnsupportedLineAndBandValues()
	{
		IChartDrawDataItem item = new RecordingDrawDataItem();
		IChartElement line = Mock.Of<IChartLineElement>();
		IChartElement band = Mock.Of<IChartBandElement>();

		// An int is not silently widened: only double, decimal and a pair of doubles are drawable.
		Throws<ArgumentException>(() => item.Add(line, 5));
		Throws<ArgumentException>(() => item.Add(band, 5));
	}

	[TestMethod]
	public void ManualRangeRequiresBothBoundsOrNeither()
	{
		IChartAxis onlyMin = new ManualRangeChartAxis { Id = "Y", AutoRange = false, MinValue = 10 };
		IChartAxis onlyMax = new ManualRangeChartAxis { Id = "Y", AutoRange = false, MaxValue = 10 };

		// Half a range cannot be drawn: the other end would silently fall back to auto-scaling.
		Throws<InvalidOperationException>(onlyMin.ValidateManualRange);
		Throws<InvalidOperationException>(onlyMax.ValidateManualRange);
	}

	[TestMethod]
	public void ManualRangeAcceptsNoBoundsAtAll()
	{
		IChartAxis axis = new ManualRangeChartAxis { Id = "Y", AutoRange = false };

		axis.ValidateManualRange();
	}

	[TestMethod]
	public void ManualRangeAcceptsOrderedBounds()
	{
		IChartAxis axis = new ManualRangeChartAxis { Id = "Y", AutoRange = false, MinValue = 5, MaxValue = 10 };

		axis.ValidateManualRange();
	}

	[TestMethod]
	public void ManualRangeRejectsEqualBounds()
	{
		IChartAxis axis = new ManualRangeChartAxis { Id = "Y", AutoRange = false, MinValue = 10, MaxValue = 10 };

		// A range of zero height has no pixels to map values onto, so it is as unusable as an inverted one.
		Throws<InvalidOperationException>(axis.ValidateManualRange);
	}

	[TestMethod]
	public void ManualRangeIgnoresBoundsWhenAutoRanging()
	{
		IChartAxis inverted = new ManualRangeChartAxis { Id = "Y", AutoRange = true, MinValue = 10, MaxValue = 5 };
		IChartAxis halfSet = new ManualRangeChartAxis { Id = "Y", AutoRange = true, MinValue = 10 };

		// Auto-ranging never reads the manual bounds, so leftovers from a previous setting are not an error.
		inverted.ValidateManualRange();
		halfSet.ValidateManualRange();
	}

	[TestMethod]
	public void ManualRangeRejectsNullAxis()
	{
		IChartAxis axis = null;

		AreEqual("axis", Throws<ArgumentNullException>(() => axis.ValidateManualRange()).ParamName);
	}

	/// <summary>
	/// Every other branch of the untyped dispatcher answers a missing value either by refusing it or by
	/// drawing nothing. A fill that never arrived is the ordinary case in a live loop, and it may not
	/// take the chart down with a <see cref="NullReferenceException"/> a caller cannot act on.
	/// </summary>
	[TestMethod]
	public void AddByElementTypeDoesNotDereferenceNullTrade()
	{
		var recorder = new RecordingDrawDataItem();
		IChartDrawDataItem item = recorder;
		IChartElement element = Mock.Of<IChartTradeElement>();

		Exception caught = null;

		try
		{
			item.Add(element, (MyTrade)null);
		}
		catch (Exception e)
		{
			caught = e;
		}

		IsFalse(caught is NullReferenceException, $"Null trade surfaced as {caught}.");
		IsNull(recorder.Call);
	}

	/// <summary>
	/// Drawing a single candle is the first thing a strategy does with a chart. The candle belongs to
	/// the bar that opened it, so it has to be grouped at its open time - at any other moment it lands
	/// somewhere else on the time axis and the chart shows a bar that never traded there - it has to
	/// reach the element with its prices and volume intact, and the data the chart is finally handed
	/// has to be the one that was filled in rather than a fresh empty one that would draw nothing.
	/// </summary>
	[TestMethod]
	public void DrawCandleGroupsItAtItsOpenTimeAndSubmitsTheFilledData()
	{
		var recorder = new RecordingDrawDataItem();
		var grouped = new List<DateTime>();

		var data = new Mock<IChartDrawData>();
		data
			.Setup(d => d.Group(It.IsAny<DateTime>()))
			.Callback<DateTime>(grouped.Add)
			.Returns(recorder);

		var drawn = new List<IChartDrawData>();

		var chart = new Mock<IChart>();
		chart.Setup(c => c.CreateData()).Returns(data.Object);
		chart.Setup(c => c.Draw(It.IsAny<IChartDrawData>())).Callback<IChartDrawData>(drawn.Add);

		var element = Mock.Of<IChartCandleElement>();
		var candle = CreateCandle();
		candle.OpenTime = new DateTime(2024, 5, 17, 10, 30, 0, DateTimeKind.Utc);

		chart.Object.Draw(element, candle);

		HasCount(1, grouped);
		AreEqual(candle.OpenTime, grouped[0]);

		AreEqual(RecordingDrawDataItem.CandleCall, recorder.Call);
		AreSame(element, recorder.Element);
		AreEqual(candle.OpenPrice, recorder.Candle.OpenPrice);
		AreEqual(candle.HighPrice, recorder.Candle.HighPrice);
		AreEqual(candle.LowPrice, recorder.Candle.LowPrice);
		AreEqual(candle.ClosePrice, recorder.Candle.ClosePrice);
		AreEqual(candle.TotalVolume, recorder.Candle.TotalVolume);

		HasCount(1, drawn);
		AreSame(data.Object, drawn[0]);
	}

	/// <summary>
	/// There is nothing to draw without an element to draw it on or a candle to draw, and drawing
	/// nothing quietly would leave the caller believing the bar reached the chart. Both are named so
	/// the caller learns which of the two it failed to pass.
	/// </summary>
	[TestMethod]
	public void DrawCandleRefusesAMissingElementOrCandle()
	{
		var chart = Mock.Of<IChart>();

		AreEqual("element", Throws<ArgumentNullException>(() => chart.Draw((IChartCandleElement)null, CreateCandle())).ParamName);
		AreEqual("candle", Throws<ArgumentNullException>(() => chart.Draw(Mock.Of<IChartCandleElement>(), (ICandleMessage)null)).ParamName);
	}

	/// <summary>
	/// Two moving averages on one area are told apart in the legend only by what their elements are
	/// called, and what tells the indicators apart is the parameter each was built with. An element
	/// that carries no title, or the same title as its neighbour, leaves the reader guessing which
	/// line is which. The element must also be attached to the chart, because one that was never added
	/// is never drawn at all.
	/// </summary>
	[TestMethod]
	public void AddIndicatorNamesTheElementAfterTheIndicatorItDraws()
	{
		var chart = new Mock<IChart>();

		chart.Setup(c => c.CreateIndicatorElement()).Returns(() =>
		{
			var created = new Mock<IChartIndicatorElement>();
			created.SetupAllProperties();
			return created.Object;
		});

		var area = new Mock<IChartArea>();
		area.SetupGet(a => a.Chart).Returns(chart.Object);

		var fast = area.Object.AddIndicator(new SimpleMovingAverage { Length = 11 });
		var slow = area.Object.AddIndicator(new SimpleMovingAverage { Length = 20 });

		IsNotEmpty(fast.FullTitle);
		AreNotEqual(fast.FullTitle, slow.FullTitle, "two indicators were given the same legend title");
		IsTrue(fast.FullTitle.Contains("11", StringComparison.Ordinal), $"'{fast.FullTitle}' does not name the length it was built with");
		IsTrue(slow.FullTitle.Contains("20", StringComparison.Ordinal), $"'{slow.FullTitle}' does not name the length it was built with");

		chart.Verify(c => c.AddElement(area.Object, fast), Times.Once);
		chart.Verify(c => c.AddElement(area.Object, slow), Times.Once);
	}

	/// <summary>
	/// Neither half of the pair is optional: without an area there is no chart to create the element
	/// on, and without an indicator there is nothing for the legend to name. Each is reported by name.
	/// </summary>
	[TestMethod]
	public void AddIndicatorRefusesAMissingAreaOrIndicator()
	{
		IChartArea area = null;

		AreEqual("area", Throws<ArgumentNullException>(() => area.AddIndicator(new SimpleMovingAverage())).ParamName);
		AreEqual("indicator", Throws<ArgumentNullException>(() => Mock.Of<IChartArea>().AddIndicator(null)).ParamName);
	}

	/// <summary>
	/// A chart drawn with no UI behind it - a backtest, a report produced on a server - has no painter
	/// provider registered at all. Asking such a setup for an indicator painter has to answer "none"
	/// rather than end the run, so the same strategy code builds its chart in both places. An indicator
	/// type that is not there is a programming error and is reported as one.
	/// </summary>
	[TestMethod]
	public void CreatePainterAnswersNoPainterWhenNothingProvidesOne()
	{
		AreEqual("type", Throws<ArgumentNullException>(() => ((IndicatorType)null).CreatePainter()).ParamName);

		IsNull(IChartExtensions.TryIndicatorPainterProvider, "a painter provider is registered, so this test cannot tell the fallback apart");

		using var type = new IndicatorType(typeof(SimpleMovingAverage));

		IsNull(type.CreatePainter());
	}

	private static Order CreateOrder(OrderStates state)
		=> new()
		{
			TransactionId = 42,
			Side = Sides.Buy,
			Price = 99.5m,
			Volume = 7m,
			State = state,
		};

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
