#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleDiagram.SampleDiagramPublic
File: EmulationDiagramStrategy.cs
Created: 2015, 12, 9, 6:53 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Designer
{
	using System;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Configuration;
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
		private string _dataPath;
		private DateTime _startDate;
		private DateTime _stopDate;
		private MarketDataSource _marketDataSource;
		private TimeSpan _candlesTimeFrame;

		[DisplayNameLoc(LocalizedStrings.Str2804Key)]
		[CategoryLoc(LocalizedStrings.Str1174Key)]
		[PropertyOrder(10)]
		public string DataPath
		{
			get { return _dataPath; }
			set
			{
				_dataPath = value;
				RaiseParametersChanged("DataPath");
			}
		}

		[DisplayNameLoc(LocalizedStrings.Str343Key)]
		[CategoryLoc(LocalizedStrings.Str1174Key)]
		[PropertyOrder(20)]
		public DateTime StartDate
		{
			get { return _startDate; }
			set
			{
				_startDate = value;
				RaiseParametersChanged("StartDate");
			}
		}

		[DisplayNameLoc(LocalizedStrings.Str345Key)]
		[CategoryLoc(LocalizedStrings.Str1174Key)]
		[PropertyOrder(30)]
		public DateTime StopDate
		{
			get { return _stopDate; }
			set
			{
				_stopDate = value;
				RaiseParametersChanged("StopDate");
			}
		}

		[DisplayNameLoc(LocalizedStrings.DataTypeKey)]
		[CategoryLoc(LocalizedStrings.Str1174Key)]
		[PropertyOrder(40)]
		public MarketDataSource MarketDataSource
		{
			get { return _marketDataSource; }
			set
			{
				_marketDataSource = value;
				RaiseParametersChanged("MarketDataSource");
			}
		}

		[DisplayNameLoc(LocalizedStrings.Str1242Key)]
		[CategoryLoc(LocalizedStrings.Str1174Key)]
		[PropertyOrder(50)]
		public TimeSpan CandlesTimeFrame
		{
			get { return _candlesTimeFrame; }
			set
			{
				_candlesTimeFrame = value;
				RaiseParametersChanged("CandlesTimeFrame");
			}
		}

		public EmulationDiagramStrategy()
		{
			DataPath = @"..\..\..\..\Samples\Testing\HistoryData\".ToFullPath();
			StartDate = new DateTime(2012, 10, 1);
			StopDate = new DateTime(2012, 10, 25);
			MarketDataSource = MarketDataSource.Candles;
			CandlesTimeFrame = TimeSpan.FromMinutes(5);
		}

		protected override bool NeedShowProperty(PropertyDescriptor propertyDescriptor)
		{
			return propertyDescriptor.DisplayName != LocalizedStrings.Portfolio && base.NeedShowProperty(propertyDescriptor);
		}

		public override void Load(SettingsStorage storage)
		{
			var compositionId = storage.GetValue<Guid>("CompositionId");
			var registry = ConfigManager.GetService<StrategiesRegistry>();
			var composition = (CompositionDiagramElement)registry.Strategies.FirstOrDefault(c => c.TypeId == compositionId);

			Composition = registry.Clone(composition);

			Id = storage.GetValue<Guid>("StrategyId");
			DataPath = storage.GetValue("DataPath", DataPath);
			StartDate = storage.GetValue("StartDate", StartDate);
			StopDate = storage.GetValue("StopDate", StopDate);
			MarketDataSource = storage.GetValue("MarketDataSource", MarketDataSource);
			CandlesTimeFrame = storage.GetValue("CandlesTimeFrame", CandlesTimeFrame);

			base.Load(storage);
		}

		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("StrategyId", Id);
			storage.SetValue("CompositionId", Composition.TypeId);
			storage.SetValue("DataPath", DataPath);
			storage.SetValue("StartDate", StartDate);
			storage.SetValue("StopDate", StopDate);
			storage.SetValue("MarketDataSource", MarketDataSource);
			storage.SetValue("CandlesTimeFrame", CandlesTimeFrame);

			base.Save(storage);
		}
	}
}