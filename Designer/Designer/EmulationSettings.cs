namespace StockSharp.Designer
{
	using System;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.ComponentModel;
	using Ecng.Configuration;
	using Ecng.Serialization;

	using StockSharp.Algo.Storages;
	using StockSharp.Localization;
	using StockSharp.Studio.Core;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[ExpandableObject]
	public class EmulationSettings : NotifiableObject, IPersistable
	{
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
		private StorageFormats _storageFormat;

		[DisplayNameLoc(LocalizedStrings.MarketDataKey)]
		[CategoryLoc(LocalizedStrings.Str1174Key)]
		[DescriptionLoc(LocalizedStrings.MarketDataStorageKey)]
		[PropertyOrder(11)]
		[Editor(typeof(MarketDataSettingsEditor), typeof(MarketDataSettingsEditor))]
		public MarketDataSettings MarketDataSettings
		{
			get { return _marketDataSettings; }
			set
			{
				_marketDataSettings = value;
				NotifyChanged("MarketDataSettings");
			}
		}

		[DisplayNameLoc(LocalizedStrings.StorageFormatKey)]
		[CategoryLoc(LocalizedStrings.Str1174Key)]
		[PropertyOrder(12)]
		public StorageFormats StorageFormat
		{
			get { return _storageFormat; }
			set
			{
				_storageFormat = value;
				NotifyChanged("StorageFormat");
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
				NotifyChanged("StartDate");
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
				NotifyChanged("StopDate");
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
				NotifyChanged("MarketDataSource");
			}
		}

		[DisplayNameLoc(LocalizedStrings.Str1242Key)]
		[CategoryLoc(LocalizedStrings.Str1174Key)]
		[DescriptionLoc(LocalizedStrings.Str1188Key)]
		[PropertyOrder(50)]
		public TimeSpan CandlesTimeFrame
		{
			get { return _candlesTimeFrame; }
			set
			{
				_candlesTimeFrame = value;
				NotifyChanged("CandlesTimeFrame");
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
				NotifyChanged("UseMarketDepths");
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
				NotifyChanged("GenerateDepths");
			}
		}

		[DisplayNameLoc(LocalizedStrings.Str1197Key)]
		[CategoryLoc(LocalizedStrings.Str1174Key)]
		[DescriptionLoc(LocalizedStrings.Str1198Key)]
		[PropertyOrder(70)]
		public int MaxDepths
		{
			get { return _maxDepths; }
			set
			{
				_maxDepths = value;
				NotifyChanged("MaxDepths");
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
				NotifyChanged("MaxVolume");
			}
		}

		[DisplayName(@"MOVE")]
		[CategoryLoc(LocalizedStrings.Str1174Key)]
		[DescriptionLoc(LocalizedStrings.Str60Key)]
		[PropertyOrder(90)]
		public bool IsSupportAtomicReRegister
		{
			get { return _isSupportAtomicReRegister; }
			set
			{
				_isSupportAtomicReRegister = value;
				NotifyChanged("IsSupportAtomicReRegister");
			}
		}

		[DisplayNameLoc(LocalizedStrings.Str1176Key)]
		[CategoryLoc(LocalizedStrings.Str1174Key)]
		[DescriptionLoc(LocalizedStrings.Str1177Key)]
		[PropertyOrder(91)]
		public bool MatchOnTouch
		{
			get { return _matchOnTouch; }
			set
			{
				_matchOnTouch = value;
				NotifyChanged("MatchOnTouch");
			}
		}

		[DisplayNameLoc(LocalizedStrings.Str161Key)]
		[CategoryLoc(LocalizedStrings.Str1174Key)]
		[DescriptionLoc(LocalizedStrings.Str1184Key)]
		[PropertyOrder(92)]
		public TimeSpan EmulatoinLatency
		{
			get { return _emulatoinLatency; }
			set
			{
				_emulatoinLatency = value;
				NotifyChanged("EmulatoinLatency");
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
				NotifyChanged("DebugLog");
			}
		}

		public EmulationSettings()
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
			StorageFormat = StorageFormats.Binary;
		}

		#region IPersistable

		public void Load(SettingsStorage storage)
		{
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

			StorageFormat = storage.GetValue("StorageFormat", StorageFormat);
		}

		public void Save(SettingsStorage storage)
		{
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

			storage.SetValue("StorageFormat", StorageFormat);
		}

		#endregion

		public override string ToString()
		{
			return string.Empty;
		}
	}
}