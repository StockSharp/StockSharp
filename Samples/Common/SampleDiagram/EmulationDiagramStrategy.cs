#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleDiagram.SampleDiagramPublic
File: EmulationDiagramStrategy.cs
Created: 2015, 12, 9, 6:53 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
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
		[DisplayNameLoc(LocalizedStrings.Str2804Key)]
		[CategoryLoc(LocalizedStrings.Str1174Key)]
		[PropertyOrder(10)]
		public string DataPath { get; set; }

		[DisplayNameLoc(LocalizedStrings.Str343Key)]
		[CategoryLoc(LocalizedStrings.Str1174Key)]
		[PropertyOrder(20)]
		public DateTime StartDate { get; set; }

		[DisplayNameLoc(LocalizedStrings.Str345Key)]
		[CategoryLoc(LocalizedStrings.Str1174Key)]
		[PropertyOrder(30)]
		public DateTime StopDate { get; set; }

		[DisplayNameLoc(LocalizedStrings.DataTypeKey)]
		[CategoryLoc(LocalizedStrings.Str1174Key)]
		[PropertyOrder(40)]
		public MarketDataSource MarketDataSource { get; set; }

		[DisplayNameLoc(LocalizedStrings.Str1242Key)]
		[CategoryLoc(LocalizedStrings.Str1174Key)]
		[PropertyOrder(50)]
		public TimeSpan CandlesTimeFrame { get; set; }

		[DisplayNameLoc(LocalizedStrings.SecurityIdKey)]
		[CategoryLoc(LocalizedStrings.Str1174Key)]
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