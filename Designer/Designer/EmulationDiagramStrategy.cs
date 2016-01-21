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
	using StockSharp.Studio.Core;
	using StockSharp.Xaml.Diagram;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	public enum MarketDataSource
	{
		Ticks,
		Candles
	}

	public class EmulationDiagramStrategy : DiagramStrategy
	{
		//private string _dataPath;
		private MarketDataSettings _marketDataSettings;
		private DateTime _startDate;
		private DateTime _stopDate;
		private MarketDataSource _marketDataSource;
		private TimeSpan _candlesTimeFrame;
		private bool _generateDepths;
		private int _maxDepths;
		private int _maxVolume;
		private bool _debugLog;
		private bool _isSupportAtomicReRegister;
		private bool _matchOnTouch;
		private TimeSpan _emulatoinLatency;
		private bool _useMarketDepths;

		//[DisplayNameLoc(LocalizedStrings.Str2804Key)]
		//[CategoryLoc(LocalizedStrings.Str1174Key)]
		//[PropertyOrder(10)]
		//public string DataPath
		//{
		//	get { return _dataPath; }
		//	set
		//	{
		//		_dataPath = value;
		//		RaiseParametersChanged("DataPath");
		//	}
		//}

		[DisplayNameLoc(LocalizedStrings.MarketDataKey)]
		[CategoryLoc(LocalizedStrings.Str1174Key)]
		[PropertyOrder(11)]
		[Editor(typeof(MarketDataSettingsEditor), typeof(MarketDataSettingsEditor))]
		public MarketDataSettings MarketDataSettings
		{
			get { return _marketDataSettings; }
			set
			{
				_marketDataSettings = value;
				RaiseParametersChanged("MarketDataSettings");
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

		[DisplayNameLoc(LocalizedStrings.MarketDepthsKey)]
		[CategoryLoc(LocalizedStrings.Str1174Key)]
		[PropertyOrder(60)]
		public bool UseMarketDepths
		{
			get { return _useMarketDepths; }
			set
			{
				_useMarketDepths = value;
				RaiseParametersChanged("UseMarketDepths");
			}
		}

		[DisplayNameLoc(LocalizedStrings.XamlStr97Key)]
		[CategoryLoc(LocalizedStrings.Str1174Key)]
		[PropertyOrder(61)]
		public bool GenerateDepths
		{
			get { return _generateDepths; }
			set
			{
				_generateDepths = value;
				RaiseParametersChanged("GenerateDepths");
			}
		}

		[DisplayNameLoc(LocalizedStrings.XamlStr291Key)]
		[CategoryLoc(LocalizedStrings.Str1174Key)]
		[PropertyOrder(70)]
		public int MaxDepths
		{
			get { return _maxDepths; }
			set
			{
				_maxDepths = value;
				RaiseParametersChanged("MaxDepths");
			}
		}

		[DisplayNameLoc(LocalizedStrings.XamlStr293Key)]
		[CategoryLoc(LocalizedStrings.Str1174Key)]
		[PropertyOrder(80)]
		public int MaxVolume
		{
			get { return _maxVolume; }
			set
			{
				_maxVolume = value;
				RaiseParametersChanged("MaxVolume");
			}
		}

		[DisplayNameLoc(LocalizedStrings.IsSupportAtomicReRegisterKey)]
		[CategoryLoc(LocalizedStrings.Str1174Key)]
		[PropertyOrder(90)]
		public bool IsSupportAtomicReRegister
		{
			get { return _isSupportAtomicReRegister; }
			set
			{
				_isSupportAtomicReRegister = value;
				RaiseParametersChanged("IsSupportAtomicReRegister");
			}
		}

		[DisplayNameLoc(LocalizedStrings.Str1176Key)]
		[CategoryLoc(LocalizedStrings.Str1174Key)]
		[PropertyOrder(91)]
		public bool MatchOnTouch
		{
			get { return _matchOnTouch; }
			set
			{
				_matchOnTouch = value;
				RaiseParametersChanged("MatchOnTouch");
			}
		}

		[DisplayNameLoc(LocalizedStrings.Str161Key)]
		[CategoryLoc(LocalizedStrings.Str1174Key)]
		[PropertyOrder(92)]
		public TimeSpan EmulatoinLatency
		{
			get { return _emulatoinLatency; }
			set
			{
				_emulatoinLatency = value;
				RaiseParametersChanged("EmulatoinLatency");
			}
		}

		[DisplayNameLoc(LocalizedStrings.XamlStr117Key)]
		[CategoryLoc(LocalizedStrings.Str1174Key)]
		[PropertyOrder(100)]
		public bool DebugLog
		{
			get { return _debugLog; }
			set
			{
				_debugLog = value;
				RaiseParametersChanged("DebugLog");
			}
		}

		public EmulationDiagramStrategy()
		{
			//DataPath = @"..\..\..\..\Samples\Testing\HistoryData\".ToFullPath();
			StartDate = new DateTime(2012, 10, 1);
			StopDate = new DateTime(2012, 10, 25);
			MarketDataSource = MarketDataSource.Candles;
			CandlesTimeFrame = TimeSpan.FromMinutes(5);
			MaxDepths = 5;
			MaxVolume = 100;
			IsSupportAtomicReRegister = true;
			MatchOnTouch = false;
			EmulatoinLatency = TimeSpan.Zero;
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
			//DataPath = storage.GetValue("DataPath", DataPath);
			StartDate = storage.GetValue("StartDate", StartDate);
			StopDate = storage.GetValue("StopDate", StopDate);
			MarketDataSource = storage.GetValue("MarketDataSource", MarketDataSource);
			CandlesTimeFrame = storage.GetValue("CandlesTimeFrame", CandlesTimeFrame);

			UseMarketDepths = storage.GetValue("UseMarketDepths", UseMarketDepths);
			GenerateDepths = storage.GetValue("GenerateDepths", GenerateDepths);
			MaxDepths = storage.GetValue("MaxDepths", MaxDepths);
			MaxVolume = storage.GetValue("MaxVolume", MaxVolume);

			IsSupportAtomicReRegister = storage.GetValue("IsSupportAtomicReRegister", IsSupportAtomicReRegister);
			MatchOnTouch = storage.GetValue("MatchOnTouch", MatchOnTouch);
			EmulatoinLatency = storage.GetValue("EmulatoinLatency", EmulatoinLatency);

			DebugLog = storage.GetValue("DebugLog", DebugLog);

			var marketDataSettings = storage.GetValue("MarketDataSettings", Guid.Empty);

			if (marketDataSettings != Guid.Empty)
				MarketDataSettings = ConfigManager.GetService<MarketDataSettingsCache>().Settings.FirstOrDefault(s => s.Id == marketDataSettings);

			base.Load(storage);
		}

		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("StrategyId", Id);
			storage.SetValue("CompositionId", Composition.TypeId);
			//storage.SetValue("DataPath", DataPath);
			storage.SetValue("StartDate", StartDate);
			storage.SetValue("StopDate", StopDate);
			storage.SetValue("MarketDataSource", MarketDataSource);
			storage.SetValue("CandlesTimeFrame", CandlesTimeFrame);

			storage.SetValue("UseMarketDepths", UseMarketDepths);
			storage.SetValue("GenerateDepths", GenerateDepths);
			storage.SetValue("MaxDepths", MaxDepths);
			storage.SetValue("MaxVolume", MaxVolume);

			storage.SetValue("IsSupportAtomicReRegister", IsSupportAtomicReRegister);
			storage.SetValue("MatchOnTouch", MatchOnTouch);
			storage.SetValue("EmulatoinLatency", EmulatoinLatency);

			storage.SetValue("DebugLog", DebugLog);

			if (MarketDataSettings != null)
				storage.SetValue("MarketDataSettings", MarketDataSettings.Id);

			base.Save(storage);
		}
	}
}