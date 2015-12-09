namespace SampleDiagram
{
	using System;
	using System.ComponentModel;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Localization;
	using StockSharp.Xaml.Diagram;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	public enum MarketDataSource
	{
		Ticks,
		Candles
	}

	public class EmulationDiagramStrategy : DiagramStrategy
	{
		[DisplayName(@"Storage path")]
		[Category("Emulation settings")]
		[PropertyOrder(10)]
		public string DataPath { get; set; }

		[DisplayName(@"Start date")]
		[Category("Emulation settings")]
		[PropertyOrder(20)]
		public DateTime StartDate { get; set; }

		[DisplayName(@"Stop date")]
		[Category("Emulation settings")]
		[PropertyOrder(30)]
		public DateTime StopDate { get; set; }

		[DisplayName(@"Data type")]
		[Category("Emulation settings")]
		[PropertyOrder(40)]
		public MarketDataSource MarketDataSource { get; set; }

		[DisplayName(@"Candles timeframe")]
		[Category("Emulation settings")]
		[PropertyOrder(50)]
		public TimeSpan CandlesTimeFrame { get; set; }

		[DisplayName(@"Security Id")]
		[Category("Emulation settings")]
		[PropertyOrder(60)]
		public string SecurityId { get; set; }

		public EmulationDiagramStrategy()
		{
			DataPath = @"..\..\..\..\Testing\HistoryData\".ToFullPath();
			SecurityId = @"RIZ2@FORTS";
			StartDate = new DateTime(2012, 10, 1);
			StopDate = new DateTime(2012, 10, 25);
			MarketDataSource = MarketDataSource.Candles;
			CandlesTimeFrame = TimeSpan.FromMinutes(5);
		}

		protected override bool NeedShowProperty(PropertyDescriptor pd)
		{
			return pd.Category != LocalizedStrings.Str436
				&& pd.Category != LocalizedStrings.Str1559
				&& pd.Category != LocalizedStrings.Str3050;
		}

		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			DataPath = storage.GetValue("DataPath", DataPath);
			SecurityId = storage.GetValue("SecurityId", SecurityId);
			StartDate = storage.GetValue("StartDate", StartDate);
			StopDate = storage.GetValue("StopDate", StopDate);
			MarketDataSource = storage.GetValue("MarketDataSource", MarketDataSource);
			CandlesTimeFrame = storage.GetValue("CandlesTimeFrame", CandlesTimeFrame);
		}

		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("DataPath", DataPath);
			storage.SetValue("SecurityId", SecurityId);
			storage.SetValue("StartDate", StartDate);
			storage.SetValue("StopDate", StopDate);
			storage.SetValue("MarketDataSource", MarketDataSource);
			storage.SetValue("CandlesTimeFrame", CandlesTimeFrame);
		}
	}
}