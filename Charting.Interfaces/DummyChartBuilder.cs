namespace StockSharp.Charting;

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

		public virtual void Load(SettingsStorage storage) { }
		void IPersistable.Save(SettingsStorage storage) { }
	}

	private class DummyPart<T> : DummyPersistable, IChartPart<T>
		where T : IChartPart<T>
	{
		public Guid Id { get; private set; } = Guid.NewGuid();

		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Id = storage.GetValue<Guid>(nameof(Id));
		}
	}

	private class DummyElement : DummyPart<IChartElement>, IChartElement
	{
		public string FullTitle { get; set; }
		public bool IsVisible { get; set; }
		public bool IsLegend { get; set; }
		public string XAxisId { get; set; }
		public string YAxisId { get; set; }
		Func<IComparable, Color?> IChartElement.Colorer { get; set; }

		public IChartArea ChartArea { get; set; }
		IChartArea IChartElement.PersistentChartArea => throw new NotSupportedException();

		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			FullTitle = storage.GetValue<string>(nameof(FullTitle));
			IsVisible = storage.GetValue<bool>(nameof(IsVisible));
			IsLegend = storage.GetValue<bool>(nameof(IsLegend));
			XAxisId = storage.GetValue<string>(nameof(XAxisId));
			YAxisId = storage.GetValue<string>(nameof(YAxisId));
		}
	}

	private class DummyActiveOrdersElement : DummyElement, IChartActiveOrdersElement
	{
		public Color BuyPendingColor { get; set; }
		public Color BuyColor { get; set; }
		public Color BuyBlinkColor { get; set; }
		public Color SellPendingColor { get; set; }
		public Color SellColor { get; set; }
		public Color SellBlinkColor { get; set; }
		public Color CancelButtonColor { get; set; }
		public Color CancelButtonBackground { get; set; }
		public Color ForegroundColor { get; set; }
		public bool IsAnimationEnabled { get; set; }

		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			BuyColor = storage.GetValue<int>(nameof(BuyColor)).ToColor();
			BuyBlinkColor = storage.GetValue<int>(nameof(BuyBlinkColor)).ToColor();
			BuyPendingColor = storage.GetValue<int>(nameof(BuyPendingColor)).ToColor();
			SellColor = storage.GetValue<int>(nameof(SellColor)).ToColor();
			SellBlinkColor = storage.GetValue<int>(nameof(SellBlinkColor)).ToColor();
			SellPendingColor = storage.GetValue<int>(nameof(SellPendingColor)).ToColor();
			ForegroundColor = storage.GetValue<int>(nameof(ForegroundColor)).ToColor();
			CancelButtonColor = storage.GetValue<int>(nameof(CancelButtonColor)).ToColor();
			CancelButtonBackground = storage.GetValue<int>(nameof(CancelButtonBackground)).ToColor();
			IsAnimationEnabled = storage.GetValue<bool>(nameof(IsAnimationEnabled));
		}
	}

	private class DummyAnnotationElement : DummyElement, IChartAnnotationElement
	{
		public ChartAnnotationTypes Type { get; set; }

		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Type = storage.GetValue<ChartAnnotationTypes>(nameof(Type));
		}
	}

	private class DummyArea : DummyPart<IChartArea>, IChartArea
	{
        public DummyArea()
        {
			_elements.Added += e => ((DummyElement)e).ChartArea = this;
			_elements.Removed += e => ((DummyElement)e).ChartArea = null;
			_elements.Clearing += () =>
			{
				_elements.ForEach(e => ((DummyElement)e).ChartArea = null);
				return true;
			};
		}

		private readonly SynchronizedList<IChartElement> _elements = [];
		INotifyList<IChartElement> IChartArea.Elements => _elements;

		INotifyList<IChartAxis> IChartArea.XAxises { get; } = new SynchronizedList<IChartAxis>();
		INotifyList<IChartAxis> IChartArea.YAxises { get; } = new SynchronizedList<IChartAxis>();

		public ChartAxisType XAxisType { get; set; } = ChartAxisType.CategoryDateTime;
		public string Title { get; set; }
		public double Height { get; set; }
		public string GroupId { get; set; }
		IChart IChartArea.Chart { get; set; }

		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Title = storage.GetValue<string>(nameof(Title));
			GroupId = storage.GetValue<string>(nameof(GroupId));
			Height = storage.GetValue<double>(nameof(Height));
			XAxisType = storage.GetValue<ChartAxisType>(nameof(XAxisType));
		}
	}

	private class DummyAxis : DummyPersistable, IChartAxis
	{
		IChartArea IChartAxis.ChartArea => throw new NotSupportedException();

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

		void INotifyPropertyChangedEx.NotifyPropertyChanged(string propertyName)
			=> throw new NotSupportedException();

		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Id = storage.GetValue<string>(nameof(Id));
			Title = storage.GetValue<string>(nameof(Title));
			IsVisible = storage.GetValue<bool>(nameof(IsVisible));
			Group = storage.GetValue<string>(nameof(Group));
			AutoRange = storage.GetValue<bool>(nameof(AutoRange));
			DrawMinorTicks = storage.GetValue<bool>(nameof(DrawMinorTicks));
			DrawMajorTicks = storage.GetValue<bool>(nameof(DrawMajorTicks));
			DrawMajorGridLines = storage.GetValue<bool>(nameof(DrawMajorGridLines));
			DrawMinorGridLines = storage.GetValue<bool>(nameof(DrawMinorGridLines));
			DrawLabels = storage.GetValue<bool>(nameof(DrawLabels));
			TextFormatting = storage.GetValue<string>(nameof(TextFormatting));
			CursorTextFormatting = storage.GetValue<string>(nameof(CursorTextFormatting));
			SubDayTextFormatting = storage.GetValue(nameof(SubDayTextFormatting), SubDayTextFormatting);
			SwitchAxisLocation = storage.GetValue<bool>(nameof(SwitchAxisLocation));
			AxisType = storage.GetValue<ChartAxisType>(nameof(AxisType));
			TimeZone = storage.GetValue<string>(nameof(TimeZone)).To<TimeZoneInfo>() ?? TimeZoneInfo.Local;
		}
	}

	private class DummyBandElement : DummyElement, IChartBandElement
	{
		DrawStyles IChartBandElement.Style { get; set; }
		IChartLineElement IChartBandElement.Line1 => throw new NotSupportedException();
		IChartLineElement IChartBandElement.Line2 => throw new NotSupportedException();
	}

	private class DummyBubbleElement : DummyLineElement
	{
	}

	private class DummyLineElement : DummyElement, IChartLineElement
	{
		public Color Color { get; set; }
		public Color AdditionalColor { get; set; }
		public int StrokeThickness { get; set; }
		public bool AntiAliasing { get; set; }
		public DrawStyles Style { get; set; }
		public bool ShowAxisMarker { get; set; }

		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			//Color = storage.GetValue<int>(nameof(Color)).ToColor();
			//AdditionalColor = storage.GetValue<int>(nameof(AdditionalColor)).ToColor();
			StrokeThickness = storage.GetValue<int>(nameof(StrokeThickness));
			AntiAliasing = storage.GetValue<bool>(nameof(AntiAliasing));
			Style = storage.GetValue<DrawStyles>(nameof(Style));
			ShowAxisMarker = storage.GetValue<bool>(nameof(ShowAxisMarker));
		}
	}

	private class DummyCandleElement : DummyElement, IChartCandleElement
	{
		public ChartCandleDrawStyles DrawStyle { get; set; }
		public Color DownFillColor { get; set; }
		public Color UpFillColor { get; set; }
		public Color DownBorderColor { get; set; }
		public Color UpBorderColor { get; set; }
		public int StrokeThickness { get; set; }
		public bool AntiAliasing { get; set; }
		public Color? LineColor { get; set; }
		public Color? AreaColor { get; set; }
		public bool ShowAxisMarker { get; set; }
		Func<DateTimeOffset, bool, bool, Color?> IChartCandleElement.Colorer { get; set; }
		public int? Timeframe2Multiplier { get; set; }
		public int? Timeframe3Multiplier { get; set; }
		public Color? FontColor { get; set; }
		public Color? Timeframe2Color { get; set; }
		public Color? Timeframe2FrameColor { get; set; }
		public Color? Timeframe3Color { get; set; }
		public Color? MaxVolumeColor { get; set; }
		public Color? ClusterLineColor { get; set; }
		public Color? ClusterSeparatorLineColor { get; set; }
		public Color? ClusterTextColor { get; set; }
		public Color? ClusterColor { get; set; }
		public Color? ClusterMaxColor { get; set; }
		public bool ShowHorizontalVolumes { get; set; }
		public bool LocalHorizontalVolumes { get; set; }
		public double HorizontalVolumeWidthFraction { get; set; }
		public Color? HorizontalVolumeColor { get; set; }
		public Color? HorizontalVolumeFontColor { get; set; }
		public decimal? PriceStep { get; set; }
		public bool DrawSeparateVolumes { get; set; }
		public Color? BuyColor { get; set; }
		public Color? SellColor { get; set; }

		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			DrawStyle = storage.GetValue<ChartCandleDrawStyles>(nameof(DrawStyle));

			UpFillColor = storage.GetValue<int>(nameof(UpFillColor)).ToColor();
			UpBorderColor = storage.GetValue<int>(nameof(UpBorderColor)).ToColor();
			DownFillColor = storage.GetValue<int>(nameof(DownFillColor)).ToColor();
			DownBorderColor = storage.GetValue<int>(nameof(DownBorderColor)).ToColor();
			LineColor = storage.GetValue<int?>(nameof(LineColor))?.ToColor();
			AreaColor = storage.GetValue<int?>(nameof(AreaColor))?.ToColor();

			StrokeThickness = storage.GetValue<int>(nameof(StrokeThickness));
			AntiAliasing = storage.GetValue<bool>(nameof(AntiAliasing));
			ShowAxisMarker = storage.GetValue<bool>(nameof(ShowAxisMarker));

			Timeframe2Multiplier = storage.GetValue<int?>(nameof(Timeframe2Multiplier));
			Timeframe3Multiplier = storage.GetValue<int?>(nameof(Timeframe3Multiplier));
			FontColor = storage.GetValue<int?>(nameof(FontColor))?.ToColor();
			Timeframe2Color = storage.GetValue<int?>(nameof(Timeframe2Color))?.ToColor();
			Timeframe2FrameColor = storage.GetValue<int?>(nameof(Timeframe2FrameColor))?.ToColor();
			Timeframe3Color = storage.GetValue<int?>(nameof(Timeframe3Color))?.ToColor();
			MaxVolumeColor = storage.GetValue<int?>(nameof(MaxVolumeColor))?.ToColor();
			ClusterSeparatorLineColor = storage.GetValue<int?>(nameof(ClusterSeparatorLineColor))?.ToColor();
			ClusterLineColor = storage.GetValue<int?>(nameof(ClusterLineColor))?.ToColor();
			ClusterTextColor = storage.GetValue<int?>(nameof(ClusterTextColor))?.ToColor();
			ClusterColor = storage.GetValue<int?>(nameof(ClusterColor))?.ToColor();
			ClusterMaxColor = storage.GetValue<int?>(nameof(ClusterMaxColor))?.ToColor();
			ShowHorizontalVolumes = storage.GetValue<bool>(nameof(ShowHorizontalVolumes));
			LocalHorizontalVolumes = storage.GetValue<bool>(nameof(LocalHorizontalVolumes));
			HorizontalVolumeWidthFraction = storage.GetValue<double>(nameof(HorizontalVolumeWidthFraction));
			HorizontalVolumeColor = storage.GetValue<int?>(nameof(HorizontalVolumeColor))?.ToColor();
			HorizontalVolumeFontColor = storage.GetValue<int?>(nameof(HorizontalVolumeFontColor))?.ToColor();
			PriceStep = storage.GetValue<decimal?>(nameof(PriceStep));
			DrawSeparateVolumes = storage.GetValue<bool>(nameof(DrawSeparateVolumes));
			BuyColor = storage.GetValue<int?>(nameof(BuyColor))?.ToColor();
			SellColor = storage.GetValue<int?>(nameof(SellColor))?.ToColor();
		}
	}

	private class DummyTransactionElement : DummyElement, IChartTransactionElement
	{
		public Color BuyColor { get; set; }
		public Color BuyStrokeColor { get; set; }
		public Color SellColor { get; set; }
		public Color SellStrokeColor { get; set; }
		public bool UseAltIcon { get; set; }
		public double DrawSize { get; set; }

		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			BuyColor = storage.GetValue<int>(nameof(BuyColor)).ToColor();
			BuyStrokeColor = storage.GetValue<int>(nameof(BuyStrokeColor)).ToColor();
			SellColor = storage.GetValue<int>(nameof(SellColor)).ToColor();
			SellStrokeColor = storage.GetValue<int>(nameof(SellStrokeColor)).ToColor();

			UseAltIcon = storage.GetValue<bool>(nameof(UseAltIcon));
			DrawSize = storage.GetValue<double>(nameof(DrawSize));
		}
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
		DrawStyles IChartIndicatorElement.DrawStyle { get; set; }
		bool IChartIndicatorElement.ShowAxisMarker { get; set; }
		bool IChartIndicatorElement.AutoAssignYAxis { get; set; }
	}

	IChartActiveOrdersElement IChartBuilder.CreateActiveOrdersElement() => new DummyActiveOrdersElement();
	IChartAnnotationElement IChartBuilder.CreateAnnotation() => new DummyAnnotationElement();
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
