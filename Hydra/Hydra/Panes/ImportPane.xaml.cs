#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Panes.HydraPublic
File: ImportPane.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Panes
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.ComponentModel;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Windows;
	using StockSharp.Hydra.Core;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Xaml.PropertyGrid;
	using StockSharp.Localization;

	public partial class ImportPane : IPane
	{
		private class FieldMapping : IPersistable
		{
			private readonly Action<dynamic, dynamic> _apply;
			private readonly Settings _settings;

			public FieldMapping(Settings settings, string name, string displayName, string description, Type type, Action<dynamic, dynamic> apply)
			{
				if (settings == null)
					throw new ArgumentNullException(nameof(settings));

				if (name.IsEmpty())
					throw new ArgumentNullException(nameof(name));
				
				if (displayName.IsEmpty())
					throw new ArgumentNullException(nameof(displayName));

				if (type == null)
					throw new ArgumentNullException(nameof(type));

				if (apply == null)
					throw new ArgumentNullException(nameof(apply));

				if (description.IsEmpty())
					description = displayName;

				_settings = settings;
				Name = name;
				DisplayName = displayName;
				Description = description;
				Type = type;
				_apply = apply;

				Values = new ObservableCollection<ImportEnumMappingWindow.MappingValue>();
				Number = -1;

				if (Type == typeof(DateTimeOffset))
					Format = "yyyy/MM/dd";
				else if (Type == typeof(TimeSpan))
					Format = "hh\\:mm\\:ss";
			}

			public int Number { get; set; }

			public string Name { get; private set; }
			public string DisplayName { get; }
			public string Description { get; private set; }

			public string Format { get; set; }

			public Type Type { get; }
			public bool IsRequired { get; set; }

			public ObservableCollection<ImportEnumMappingWindow.MappingValue> Values { get; }

			public object DefaultValue { get; set; }

			public void Load(SettingsStorage storage)
			{
				Name = storage.GetValue<string>(nameof(Name));
				Number = storage.GetValue<int>(nameof(Number));
				Values.AddRange(storage.GetValue<SettingsStorage[]>(nameof(Values)).Select(s => s.Load<ImportEnumMappingWindow.MappingValue>()));
				DefaultValue = storage.GetValue<object>(nameof(DefaultValue));
				Format = storage.GetValue<string>(nameof(Format));
			}

			void IPersistable.Save(SettingsStorage storage)
			{
				storage.SetValue(nameof(Name), Name);
				storage.SetValue(nameof(Number), Number);
				storage.SetValue(nameof(Values), Values.Select(v => v.Save()).ToArray());
				storage.SetValue(nameof(DefaultValue), DefaultValue);
				storage.SetValue(nameof(Format), Format);
			}

			public void ApplyFileValue(object instance, string value)
			{
				if (value.IsEmpty())
				{
					ApplyDefaultValue(instance);
					return;
				}

				if (Values.Count > 0)
				{
					var v = Values.FirstOrDefault(vl => vl.ValueFile.CompareIgnoreCase(value));

					if (v != null)
					{
						ApplyValue(instance, v.ValueStockSharp);
						return;
					}
				}

				ApplyValue(instance, value);
			}

			public void ApplyDefaultValue(object instance)
			{
				ApplyValue(instance, DefaultValue);
			}

			private void ApplyValue(object instance, object value)
			{
				if (Type == typeof(decimal))
				{
					var str = value as string;

					if (str != null)
					{
						str = str.Replace(",", ".").Replace(" ", string.Empty).ReplaceWhiteSpaces().Trim();

						if (str.IsEmpty())
							return;

						value = str;
					}
				}
				else if (Type == typeof(DateTimeOffset))
				{
					var str = value as string;

					if (str != null)
					{
						var dto = str.ToDateTimeOffset(Format);

						if (dto.Offset == TimeSpan.Zero)
						{
							dto = dto.UtcDateTime.ApplyTimeZone(_settings.TimeZone);
						}

						value = dto;
					}
				}
				else if (Type == typeof(TimeSpan))
				{
					var str = value as string;

					if (str != null)
						value = str.ToTimeSpan(Format);
				}

				_apply(instance, value.To(Type));
			}

			public override string ToString()
			{
				return Name;
			}
		}

		[DisplayNameLoc(LocalizedStrings.Str2842Key)]
		private class Settings : NotifiableObject, IPersistable
		{
			public Settings()
			{
				CandleSettings = new CandleSeries { CandleType = typeof(TimeFrameCandle), Arg = TimeSpan.FromMinutes(1) };
			}

			private string _path;

			[CategoryLoc(LocalizedStrings.Str1559Key)]
			[DisplayNameLoc(LocalizedStrings.Str2804Key)]
			[DescriptionLoc(LocalizedStrings.Str2843Key)]
			[Editor(typeof(FileBrowserEditor), typeof(FileBrowserEditor))]
			public string Path
			{
				get { return _path; }
				set
				{
					_path = value;
					NotifyChanged(nameof(Path));
				}
			}

			private string _columnSeparator;

			[CategoryLoc(LocalizedStrings.Str1559Key)]
			[DisplayNameLoc(LocalizedStrings.Str2844Key)]
			[DescriptionLoc(LocalizedStrings.Str2845Key)]
			public string ColumnSeparator
			{
				get { return _columnSeparator; }
				set
				{
					_columnSeparator = value;
					NotifyChanged(nameof(ColumnSeparator));
				}
			}

			//[DisplayName("Разделитель строчек")]
			//[Description("Разделитель строчек.")]
			//public string RowSeparator { get; set; }

			private int _skipFromHeader;

			[CategoryLoc(LocalizedStrings.Str1559Key)]
			[DisplayNameLoc(LocalizedStrings.Str2846Key)]
			[DescriptionLoc(LocalizedStrings.Str2847Key)]
			public int SkipFromHeader
			{
				get { return _skipFromHeader; }
				set
				{
					_skipFromHeader = value;
					NotifyChanged(nameof(SkipFromHeader));
				}
			}

			//[DisplayName("Отступ с конца")]
			//[Description("Количество строчек, которые нужно пропустить с конца файла (если они несут мета информацию).")]
			//public int SkipFromFooter { get; set; }

			private IMarketDataDrive _drive;

			[CategoryLoc(LocalizedStrings.Str1559Key)]
			[DisplayNameLoc(LocalizedStrings.Str2237Key)]
			[DescriptionLoc(LocalizedStrings.Str2238Key)]
			[Editor(typeof(DriveComboBoxEditor), typeof(DriveComboBoxEditor))]
			public IMarketDataDrive Drive
			{
				get { return _drive; }
				set
				{
					_drive = value;
					NotifyChanged(nameof(Drive));
				}
			}

			private TimeZoneInfo _timeZone = TimeZoneInfo.Utc;

			[CategoryLoc(LocalizedStrings.Str1559Key)]
			[DisplayNameLoc(LocalizedStrings.TimeZoneKey)]
			[DescriptionLoc(LocalizedStrings.TimeZoneKey, true)]
			public TimeZoneInfo TimeZone
			{
				get { return _timeZone; }
				set
				{
					if (value == null)
						throw new ArgumentNullException(nameof(value));

					_timeZone = value;
					NotifyChanged(nameof(TimeZone));
				}
			}

			[CategoryLoc(LocalizedStrings.Str1559Key)]
			[DisplayNameLoc(LocalizedStrings.Str2239Key)]
			[DescriptionLoc(LocalizedStrings.Str2848Key)]
			public StorageFormats Format { get; set; }

			private CandleSeries _candleSettings;

			[CategoryLoc(LocalizedStrings.CandlesKey)]
			[DisplayNameLoc(LocalizedStrings.CandlesKey)]
			[DescriptionLoc(LocalizedStrings.Str2849Key)]
			[Editor(typeof(CandleSettingsEditor), typeof(CandleSettingsEditor))]
			public CandleSeries CandleSettings
			{
				get { return _candleSettings; }
				set
				{
					_candleSettings = value;
					NotifyChanged(nameof(CandleSettings));
				}
			}

			public void Load(SettingsStorage storage)
			{
				Path = storage.GetValue<string>(nameof(Path));
				ColumnSeparator = storage.GetValue<string>(nameof(ColumnSeparator));
				//RowSeparator = storage.GetValue<string>(nameof(RowSeparator));
				SkipFromHeader = storage.GetValue<int>(nameof(SkipFromHeader));
				//SkipFromFooter = storage.GetValue<int>(nameof(SkipFromFooter));
				Format = storage.GetValue<StorageFormats>(nameof(Format));

				if (storage.ContainsKey("Drive"))
					Drive = DriveCache.Instance.GetDrive(storage.GetValue<string>(nameof(Drive)));

				TimeZone = TimeZoneInfo.FindSystemTimeZoneById(storage.GetValue<string>(nameof(TimeZone)));

				CandleSettings = storage.GetValue(nameof(CandleSettings), CandleSettings);
			}

			void IPersistable.Save(SettingsStorage storage)
			{
				storage.SetValue(nameof(Path), Path);
				storage.SetValue(nameof(ColumnSeparator), ColumnSeparator);
				//storage.SetValue(nameof(RowSeparator), RowSeparator);
				storage.SetValue(nameof(SkipFromHeader), SkipFromHeader);
				//storage.SetValue(nameof(SkipFromFooter), SkipFromFooter);
				storage.SetValue(nameof(Format), Format.To<string>());

				if (Drive != null)
					storage.SetValue(nameof(Drive), Drive.Path);

				storage.SetValue(nameof(TimeZone), TimeZone.Id);
				storage.SetValue(nameof(CandleSettings), CandleSettings);
			}
		}

		private readonly ObservableCollection<FieldMapping> _fields = new ObservableCollection<FieldMapping>();
		private readonly Settings _settings;
		private readonly BackgroundWorker _worker;
		private readonly HydraEntityRegistry _entityRegistry;
		private readonly LogManager _logManager = ConfigManager.GetService<LogManager>();

		public ImportPane()
		{
			InitializeComponent();

			_settings = new Settings
			{
				ColumnSeparator = ",",
				//RowSeparator = Environment.NewLine,
			};

			SettingsGrid.SelectedObject = _settings;
			FieldsGrid.ItemsSource = _fields;

			_worker = new BackgroundWorker { WorkerSupportsCancellation = true, WorkerReportsProgress = true };
			_worker.DoWork += OnDoWork;
			_worker.ProgressChanged += OnProgressChanged;
			_worker.RunWorkerCompleted += OnRunWorkerCompleted;

			_entityRegistry = ConfigManager.GetService<HydraEntityRegistry>();
		}

		public ExecutionTypes? ExecutionType { get; set; }

		private Type _dataType;

		public Type DataType
		{
			get { return _dataType; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_dataType = value;

				_fields.Clear();

				var secCodeDescr = LocalizedStrings.Str2850;
				var boardCodeDescr = LocalizedStrings.Str2851;

				var dateDescr = LocalizedStrings.Str2852;
				var timeDescr = LocalizedStrings.Str2853;

				if (value == typeof(SecurityMessage))
				{
					_title = LocalizedStrings.Securities.ToLowerInvariant();

					_fields.Add(new FieldMapping(_settings, "SecurityCode", LocalizedStrings.Security, secCodeDescr, typeof(string), (i, v) => SetSecCode(i, v)) { IsRequired = true });
					_fields.Add(new FieldMapping(_settings, "BoardCode", LocalizedStrings.Board, boardCodeDescr, typeof(string), (i, v) => SetBoardCode(i, v)) { IsRequired = true });

					_fields.Add(new FieldMapping(_settings, "Name", LocalizedStrings.Name, LocalizedStrings.Str362, typeof(string), (i, v) => i.Name = v));
					_fields.Add(new FieldMapping(_settings, "PriceStep", LocalizedStrings.PriceStep, LocalizedStrings.MinPriceStep, typeof(decimal), (i, v) => i.PriceStep = v));
					_fields.Add(new FieldMapping(_settings, "Decimals", LocalizedStrings.Decimals, LocalizedStrings.Str548, typeof(int), (i, v) => i.Decimals = v));
					_fields.Add(new FieldMapping(_settings, "VolumeStep", LocalizedStrings.VolumeStep, LocalizedStrings.Str366, typeof(decimal), (i, v) => i.VolumeStep = v));
					_fields.Add(new FieldMapping(_settings, "Multiplier", LocalizedStrings.Str330, LocalizedStrings.LotVolume, typeof(decimal), (i, v) => i.Multiplier = v));
					_fields.Add(new FieldMapping(_settings, "SecurityType", LocalizedStrings.Type, LocalizedStrings.Str360, typeof(SecurityTypes), (i, v) => i.SecurityType = v));
				}
				else if (value == typeof(ExecutionMessage))
				{
					switch (ExecutionType)
					{
						case ExecutionTypes.Tick:
						{
							_title = LocalizedStrings.Str985.ToLowerInvariant();

							_fields.Add(new FieldMapping(_settings, "SecurityCode", LocalizedStrings.Security, secCodeDescr, typeof(string), (i, v) => SetSecCode(i, v)) { IsRequired = true });
							_fields.Add(new FieldMapping(_settings, "BoardCode", LocalizedStrings.Board, boardCodeDescr, typeof(string), (i, v) => SetBoardCode(i, v)) { IsRequired = true });

							_fields.Add(new FieldMapping(_settings, "Id", LocalizedStrings.Id, string.Empty, typeof(long), (i, v) => i.TradeId = v));
							_fields.Add(new FieldMapping(_settings, "StringId", LocalizedStrings.Str2856, string.Empty, typeof(string), (i, v) => i.TradeStringId = v));
							_fields.Add(new FieldMapping(_settings, "Date", LocalizedStrings.Date, dateDescr, typeof(DateTimeOffset), (i, v) => i.ServerTime = v + i.ServerTime.TimeOfDay) { IsRequired = true });
							_fields.Add(new FieldMapping(_settings, "Time", LocalizedStrings.Time, timeDescr, typeof(TimeSpan), (i, v) => i.ServerTime += v));
							_fields.Add(new FieldMapping(_settings, "Price", LocalizedStrings.Price, string.Empty, typeof(decimal), (i, v) => i.TradePrice = v) { IsRequired = true });
							_fields.Add(new FieldMapping(_settings, "Volume", LocalizedStrings.Volume, string.Empty, typeof(decimal), (i, v) => i.Volume = v) { IsRequired = true });
							_fields.Add(new FieldMapping(_settings, "OriginSide", LocalizedStrings.Str329, LocalizedStrings.Str149, typeof(Sides), (i, v) => i.OriginSide = v));
							_fields.Add(new FieldMapping(_settings, "OI", LocalizedStrings.Str150, LocalizedStrings.Str151, typeof(decimal), (i, v) => i.OpenInterest = v));
							_fields.Add(new FieldMapping(_settings, "IsSystem", LocalizedStrings.Str342, LocalizedStrings.Str140, typeof(bool), (i, v) => i.IsSystem = v));
							_fields.Add(new FieldMapping(_settings, "IsUpTick", LocalizedStrings.Str157, LocalizedStrings.Str158, typeof(bool), (i, v) => i.IsUpTick = v));

							break;
						}
						case ExecutionTypes.OrderLog:
						{
							_title = LocalizedStrings.Str2858;

							_fields.Add(new FieldMapping(_settings, "SecurityCode", LocalizedStrings.Security, secCodeDescr, typeof(string), (i, v) => SetSecCode(i, v)) { IsRequired = true });
							_fields.Add(new FieldMapping(_settings, "BoardCode", LocalizedStrings.Board, boardCodeDescr, typeof(string), (i, v) => SetBoardCode(i, v)) { IsRequired = true });

							_fields.Add(new FieldMapping(_settings, "Id", LocalizedStrings.Id, LocalizedStrings.OrderId, typeof(long), (i, v) => i.OrderId = v) { IsRequired = true });
							_fields.Add(new FieldMapping(_settings, "Date", LocalizedStrings.Date, dateDescr, typeof(DateTimeOffset), (i, v) => i.ServerTime = v + i.ServerTime.TimeOfDay) { IsRequired = true });
							_fields.Add(new FieldMapping(_settings, "Time", LocalizedStrings.Time, timeDescr, typeof(TimeSpan), (i, v) => i.ServerTime += v));
							_fields.Add(new FieldMapping(_settings, "Price", LocalizedStrings.Price, LocalizedStrings.OrderPrice, typeof(decimal), (i, v) => i.Price = v) { IsRequired = true });
							_fields.Add(new FieldMapping(_settings, "Volume", LocalizedStrings.Volume, LocalizedStrings.OrderVolume, typeof(decimal), (i, v) => i.Volume = v) { IsRequired = true });
							_fields.Add(new FieldMapping(_settings, "Side", LocalizedStrings.Str128, LocalizedStrings.Str129, typeof(Sides), (i, v) => i.Side = v) { IsRequired = true });
							_fields.Add(new FieldMapping(_settings, "IsSystem", LocalizedStrings.Str342, LocalizedStrings.Str140, typeof(bool), (i, v) => i.IsSystem = v));
							_fields.Add(new FieldMapping(_settings, "Action", LocalizedStrings.Str722, LocalizedStrings.Str134, typeof(OrderStates), (i, v) => i.OrderState = v) { IsRequired = true });
							_fields.Add(new FieldMapping(_settings, "TradeId", LocalizedStrings.Str723, LocalizedStrings.Str145, typeof(long), (i, v) => i.TradeId = v) { IsRequired = true });
							_fields.Add(new FieldMapping(_settings, "TradePrice", LocalizedStrings.Str724, LocalizedStrings.Str147, typeof(decimal), (i, v) => i.TradePrice = v) { IsRequired = true });
							_fields.Add(new FieldMapping(_settings, "OI", LocalizedStrings.Str150, LocalizedStrings.Str151, typeof(decimal), (i, v) => i.OpenInterest = v));
							break;
						}
						case ExecutionTypes.Transaction:
						{
							_title = LocalizedStrings.Transactions;

							_fields.Add(new FieldMapping(_settings, "SecurityCode", LocalizedStrings.Security, secCodeDescr, typeof(string), (i, v) => SetSecCode(i, v)) { IsRequired = true });
							_fields.Add(new FieldMapping(_settings, "BoardCode", LocalizedStrings.Board, boardCodeDescr, typeof(string), (i, v) => SetBoardCode(i, v)) { IsRequired = true });
							_fields.Add(new FieldMapping(_settings, "Date", LocalizedStrings.Date, dateDescr, typeof(DateTimeOffset), (i, v) => i.ServerTime = v + i.ServerTime.TimeOfDay) { IsRequired = true });
							_fields.Add(new FieldMapping(_settings, "Time", LocalizedStrings.Time, timeDescr, typeof(TimeSpan), (i, v) => i.ServerTime += v));
							_fields.Add(new FieldMapping(_settings, "Portfolio", LocalizedStrings.Portfolio, LocalizedStrings.PortfolioName, typeof(string), (i, v) => i.PortfolioName = v) { IsRequired = true });
							_fields.Add(new FieldMapping(_settings, "TransactionId", LocalizedStrings.TransactionId, LocalizedStrings.TransactionId, typeof(long), (i, v) => i.TransactionId = v));
							_fields.Add(new FieldMapping(_settings, "OrderId", LocalizedStrings.Id, LocalizedStrings.OrderId, typeof(long), (i, v) => i.OrderId = v));
							_fields.Add(new FieldMapping(_settings, "Price", LocalizedStrings.Price, LocalizedStrings.OrderPrice, typeof(decimal), (i, v) => i.Price = v) { IsRequired = true });
							_fields.Add(new FieldMapping(_settings, "Volume", LocalizedStrings.Volume, LocalizedStrings.OrderVolume, typeof(decimal), (i, v) => i.Volume = v) { IsRequired = true });
							_fields.Add(new FieldMapping(_settings, "Balance", LocalizedStrings.Str1325, LocalizedStrings.Str131, typeof(decimal), (i, v) => i.Balance = v) { IsRequired = true });
							_fields.Add(new FieldMapping(_settings, "Side", LocalizedStrings.Str329, LocalizedStrings.Str129, typeof(Sides), (i, v) => i.Side = v));
							_fields.Add(new FieldMapping(_settings, "OrderType", LocalizedStrings.Str132, LocalizedStrings.Str133, typeof(OrderTypes), (i, v) => i.OrderType = v) { IsRequired = true });
							_fields.Add(new FieldMapping(_settings, "OrderState", LocalizedStrings.State, LocalizedStrings.Str134, typeof(OrderStates), (i, v) => i.OrderState = v) { IsRequired = true });
							_fields.Add(new FieldMapping(_settings, "TradeId", LocalizedStrings.Str723, LocalizedStrings.Str145, typeof(long), (i, v) => i.TradeId = v) { IsRequired = true });
							_fields.Add(new FieldMapping(_settings, "TradePrice", LocalizedStrings.Str724, LocalizedStrings.Str147, typeof(decimal), (i, v) => i.TradePrice = v) { IsRequired = true });

							break;
						}
						default:
							throw new ArgumentOutOfRangeException();
					}
				}
				else if (value == typeof(CandleMessage))
				{
					_title = LocalizedStrings.Str2859;

					_fields.Add(new FieldMapping(_settings, "SecurityCode", LocalizedStrings.Security, secCodeDescr, typeof(string), (i, v) => SetSecCode(i, v)) { IsRequired = true });
					_fields.Add(new FieldMapping(_settings, "BoardCode", LocalizedStrings.Board, boardCodeDescr, typeof(string), (i, v) => SetBoardCode(i, v)) { IsRequired = true });

					_fields.Add(new FieldMapping(_settings, "Date", LocalizedStrings.Date, dateDescr, typeof(DateTimeOffset), (i, v) =>
					{
						i.OpenTime = v + i.OpenTime.TimeOfDay;
						i.CloseTime = v + i.CloseTime.TimeOfDay;
					}) { IsRequired = true });
					_fields.Add(new FieldMapping(_settings, "OpenTime", LocalizedStrings.Str2860, LocalizedStrings.CandleOpenTime, typeof(TimeSpan), (i, v) => i.OpenTime += v));
					_fields.Add(new FieldMapping(_settings, "CloseTime", LocalizedStrings.Str2861, LocalizedStrings.CandleCloseTime, typeof(TimeSpan), (i, v) => i.CloseTime += v));
					_fields.Add(new FieldMapping(_settings, "OI", LocalizedStrings.Str150, string.Empty, typeof(decimal), (i, v) => i.OpenInterest = (decimal)v));
					_fields.Add(new FieldMapping(_settings, "OpenPrice", "O", LocalizedStrings.Str80, typeof(decimal), (i, v) => i.OpenPrice = v) { IsRequired = true });
					_fields.Add(new FieldMapping(_settings, "HighPrice", "H", LocalizedStrings.Str82, typeof(decimal), (i, v) => i.HighPrice = v) { IsRequired = true });
					_fields.Add(new FieldMapping(_settings, "LowPrice", "L", LocalizedStrings.Str84, typeof(decimal), (i, v) => i.LowPrice = v) { IsRequired = true });
					_fields.Add(new FieldMapping(_settings, "ClosePrice", "C", LocalizedStrings.Str86, typeof(decimal), (i, v) => i.ClosePrice = v) { IsRequired = true });
					_fields.Add(new FieldMapping(_settings, "Volume", "V", LocalizedStrings.TotalCandleVolume, typeof(decimal), (i, v) => i.TotalVolume = v) { IsRequired = true });
					//_fields.Add(new FieldMapping(_settings, "Arg", "Параметр", string.Empty, typeof(object), (i, v) => i.Arg = v) { IsRequired = true });
					_fields.Add(new FieldMapping(_settings, "UpTicks", LocalizedStrings.TickUp, LocalizedStrings.TickUpCount, typeof(decimal), (i, v) => i.UpTicks = v) { IsRequired = false });
					_fields.Add(new FieldMapping(_settings, "DownTicks", LocalizedStrings.TickDown, LocalizedStrings.TickDownCount, typeof(decimal), (i, v) => i.DownTicks = v) { IsRequired = false });
					_fields.Add(new FieldMapping(_settings, "TotalTicks", LocalizedStrings.Ticks, LocalizedStrings.TickCount, typeof(decimal), (i, v) => i.TotalTicks = v) { IsRequired = false });
				}
				else if (value == typeof(QuoteChangeMessage))
				{
					_title = LocalizedStrings.Str2862;

					_fields.Add(new FieldMapping(_settings, "SecurityCode", LocalizedStrings.Security, secCodeDescr, typeof(string), (i, v) => SetSecCode(i, v)) { IsRequired = true });
					_fields.Add(new FieldMapping(_settings, "BoardCode", LocalizedStrings.Board, boardCodeDescr, typeof(string), (i, v) => SetBoardCode(i, v)) { IsRequired = true });

					_fields.Add(new FieldMapping(_settings, "Date", LocalizedStrings.Date, dateDescr, typeof(DateTimeOffset), (i, v) => i.ServerTime = v + i.ServerTime.TimeOfDay) { IsRequired = true });
					_fields.Add(new FieldMapping(_settings, "Time", LocalizedStrings.Time, timeDescr, typeof(TimeSpan), (i, v) => i.ServerTime += v));
					_fields.Add(new FieldMapping(_settings, "Price", LocalizedStrings.Price, LocalizedStrings.Str275, typeof(decimal), (i, v) => i.Price = v) { IsRequired = true });
					_fields.Add(new FieldMapping(_settings, "Volume", LocalizedStrings.Volume, LocalizedStrings.Str276, typeof(decimal), (i, v) => i.Volume = v) { IsRequired = true });
					_fields.Add(new FieldMapping(_settings, "Side", LocalizedStrings.Str128, LocalizedStrings.Str277, typeof(Sides), (i, v) => i.Side = v) { IsRequired = true });
				}
				else if (value == typeof(Level1ChangeMessage))
				{
					_title = "level 1";

					_fields.Add(new FieldMapping(_settings, "SecurityCode", LocalizedStrings.Security, secCodeDescr, typeof(string), (i, v) => SetSecCode(i, v)) { IsRequired = true });
					_fields.Add(new FieldMapping(_settings, "BoardCode", LocalizedStrings.Board, boardCodeDescr, typeof(string), (i, v) => SetBoardCode(i, v)) { IsRequired = true });

					_fields.Add(new FieldMapping(_settings, "Date", LocalizedStrings.Date, dateDescr, typeof(DateTimeOffset), (i, v) => i.ServerTime = v + i.ServerTime.TimeOfDay) { IsRequired = true });
					_fields.Add(new FieldMapping(_settings, "Time", LocalizedStrings.Time, timeDescr, typeof(TimeSpan), (i, v) => i.ServerTime += v));

					_fields.Add(new FieldMapping(_settings, "LastTradeId", Level1Fields.LastTradeId.GetDisplayName(), string.Empty, typeof(long), (i, v) => i.Changes.Add(Level1Fields.LastTradeId, v)));
					_fields.Add(new FieldMapping(_settings, "LastTradeTime", Level1Fields.LastTradeTime.GetDisplayName(), string.Empty, typeof(DateTimeOffset), (i, v) => i.Changes.Add(Level1Fields.LastTradeTime, v)));
					_fields.Add(new FieldMapping(_settings, "BestBidTime", Level1Fields.BestBidTime.GetDisplayName(), string.Empty, typeof(DateTimeOffset), (i, v) => i.Changes.Add(Level1Fields.BestBidTime, v)));
					_fields.Add(new FieldMapping(_settings, "BestAskTime", Level1Fields.BestAskTime.GetDisplayName(), string.Empty, typeof(DateTimeOffset), (i, v) => i.Changes.Add(Level1Fields.BestAskTime, v)));
					_fields.Add(new FieldMapping(_settings, "BidsCount", Level1Fields.BidsCount.GetDisplayName(), string.Empty, typeof(int), (i, v) => i.Changes.Add(Level1Fields.BidsCount, v)));
					_fields.Add(new FieldMapping(_settings, "AsksCount", Level1Fields.AsksCount.GetDisplayName(), string.Empty, typeof(int), (i, v) => i.Changes.Add(Level1Fields.AsksCount, v)));
					_fields.Add(new FieldMapping(_settings, "TradesCount", Level1Fields.TradesCount.GetDisplayName(), string.Empty, typeof(int), (i, v) => i.Changes.Add(Level1Fields.TradesCount, v)));

					foreach (var f in new[]
					{
						Level1Fields.LastTradePrice,
						Level1Fields.LastTradeVolume,
						Level1Fields.BestBidPrice,
						Level1Fields.BestBidVolume,
						Level1Fields.BestAskPrice,
						Level1Fields.BestAskVolume,
						Level1Fields.BidsVolume,
						Level1Fields.AsksVolume,
						Level1Fields.HighBidPrice,
						Level1Fields.LowAskPrice,
						Level1Fields.MaxPrice,
						Level1Fields.MinPrice,
						Level1Fields.OpenInterest,
						Level1Fields.OpenPrice,
						Level1Fields.HighPrice,
						Level1Fields.LowPrice,
						Level1Fields.ClosePrice,
						Level1Fields.Volume,
						Level1Fields.HistoricalVolatility,
						Level1Fields.ImpliedVolatility,
						Level1Fields.Delta,
						Level1Fields.Gamma,
						Level1Fields.Theta,
						Level1Fields.Vega,
						Level1Fields.Rho,
						Level1Fields.TheorPrice,
						Level1Fields.Change,
						Level1Fields.AccruedCouponIncome,
						Level1Fields.AveragePrice,
						Level1Fields.MarginBuy,
						Level1Fields.MarginSell,
						Level1Fields.SettlementPrice,
						Level1Fields.VWAP,
						Level1Fields.Yield
					})
					{
						var field = f;

						_fields.Add(new FieldMapping(_settings, field.ToString(), field.GetDisplayName(), string.Empty, typeof(decimal), (i, v) =>
						{
							if (v == 0m)
								return;

							i.Changes.Add(field, v);
						}));
					}
				}
				else if (value == typeof(NewsMessage))
				{
					_title = LocalizedStrings.Str2863;

					_fields.Add(new FieldMapping(_settings, "Id", LocalizedStrings.Id, string.Empty, typeof(string), (i, v) => i.Id = v) { IsRequired = true });
					_fields.Add(new FieldMapping(_settings, "SecurityCode", LocalizedStrings.Security, secCodeDescr, typeof(string), (i, v) => i.Security.Code = v));
					_fields.Add(new FieldMapping(_settings, "BoardCode", LocalizedStrings.Board, boardCodeDescr, typeof(string), (i, v) => i.BoardCode = v));
					_fields.Add(new FieldMapping(_settings, "Date", LocalizedStrings.Date, dateDescr, typeof(DateTimeOffset), (i, v) => i.ServerTime = v + i.ServerTime.TimeOfDay) { IsRequired = true });
					_fields.Add(new FieldMapping(_settings, "Time", LocalizedStrings.Time, timeDescr, typeof(TimeSpan), (i, v) => i.ServerTime += v));
					_fields.Add(new FieldMapping(_settings, "Headline", LocalizedStrings.Str215, LocalizedStrings.Str215, typeof(string), (i, v) => i.Headline = v));
					_fields.Add(new FieldMapping(_settings, "Story", LocalizedStrings.Str217, LocalizedStrings.Str218, typeof(string), (i, v) => i.Story = v));
					_fields.Add(new FieldMapping(_settings, "Source", LocalizedStrings.Str213, LocalizedStrings.Str214, typeof(string), (i, v) => i.Source = v));
					_fields.Add(new FieldMapping(_settings, "Url", LocalizedStrings.Str221, LocalizedStrings.Str222, typeof(Uri), (i, v) => i.Url = v));
				}
				else
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1655);
			}
		}

		private static void SetSecCode(dynamic message, string code)
		{
			SecurityId securityId = message.SecurityId;
			securityId.SecurityCode = code;
			message.SecurityId = securityId;
		}

		private static void SetBoardCode(dynamic message, string code)
		{
			SecurityId securityId = message.SecurityId;
			securityId.BoardCode = code;
			message.SecurityId = securityId;
		}

		void IPersistable.Load(SettingsStorage storage)
		{
			DataType = storage.GetValue<Type>("DataType");
			ExecutionType = storage.GetValue<ExecutionTypes?>("ExecutionType");

			foreach (var fieldSettings in storage.GetValue<SettingsStorage[]>("Fields"))
			{
				var fieldName = fieldSettings.GetValue<string>("Name");
				var field = _fields.FirstOrDefault(f => f.Name.CompareIgnoreCase(fieldName));

				if (field != null)
					field.Load(fieldSettings);
			}

			_settings.Load(storage.GetValue<SettingsStorage>("Settings"));
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			storage.SetValue("DataType", DataType.GetTypeName(false));
			storage.SetValue("ExecutionType", ExecutionType.To<string>());
			storage.SetValue("Fields", _fields.Select(f => f.Save()).ToArray());
			storage.SetValue("Settings", _settings.Save());
		}

		private string _title;

		string IPane.Title => LocalizedStrings.Str2864 + " '{0}'".Put(_title);

		Uri IPane.Icon => null;

		bool IPane.IsValid => DataType != null;

		private void ImportBtn_OnClick(object sender, RoutedEventArgs e)
		{
			if (_worker.IsBusy)
			{
				ImportBtn.IsEnabled = false;
				_worker.CancelAsync();
			}
			else
			{
				var mbBuilder = new MessageBoxBuilder()
					.Owner(this)
					.Error();

				if (_settings.Path.IsEmpty())
				{
					mbBuilder.Text(LocalizedStrings.Str2865).Show();
					return;
				}

				if (!File.Exists(_settings.Path))
				{
					mbBuilder.Text(LocalizedStrings.Str2866Params.Put(_settings.Path)).Show();
					return;
				}

				var field = _fields.FirstOrDefault(f => f.IsRequired && f.Number == -1 && (f.DefaultValue == null || f.DefaultValue.Equals(string.Empty)));

				if (field != null)
				{
					mbBuilder.Text(LocalizedStrings.Str2867Params.Put(field.DisplayName)).Show();
					return;
				}

				field = _fields.FirstOrDefault(f => f.Number > -1 && f.Type.IsEnum && f.Values.IsEmpty());

				if (field != null)
				{
					mbBuilder.Text(LocalizedStrings.Str2868Params.Put(field.DisplayName)).Show();
					return;
				}

				ImportBtn.IsEnabled = false;
				ImportBtnText.Text = LocalizedStrings.Str2890;

				_worker.RunWorkerAsync();
			}
		}

		private void OnProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			ProgressBar.Value = e.ProgressPercentage;
		}

		private void OnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Error != null)
				e.Error.LogError();

			ImportBtnText.Text = LocalizedStrings.Import;
			ImportBtn.IsEnabled = true;

			ProgressBar.Value = 0;
		}

		private void OnDoWork(object sender, DoWorkEventArgs e)
		{
			this.GuiAsync(() => ImportBtn.IsEnabled = true);

			var columnSeparator = _settings.ColumnSeparator.ReplaceIgnoreCase("TAB", "\t");

			var drive = _settings.Drive ?? DriveCache.Instance.DefaultDrive;

			var buffer = new List<dynamic>();

			CultureInfo.InvariantCulture.DoInCulture(() =>
			{
				using (var reader = new StreamReader(_settings.Path))
				{
					var len = reader.BaseStream.Length;
					var prevPercent = 0;

					var skipLines = _settings.SkipFromHeader;
					var lineIndex = 0;

					while (!reader.EndOfStream && !_worker.CancellationPending)
					{
						var line = reader.ReadLine();

						if (skipLines > 0)
						{
							skipLines--;
							continue;
						}

						var cells = line.Split(columnSeparator, false);

						dynamic instance = DataType == typeof(QuoteChangeMessage)
							? new TimeQuoteChange()
							: (DataType == typeof(CandleMessage)
								? _settings.CandleSettings.CandleType.ToCandleMessageType()
								: DataType
							).CreateInstance<object>();

						var secMsg = instance as SecurityMessage;

						foreach (var field in _fields)
						{
							if (field.Number >= cells.Length)
								throw new InvalidOperationException(LocalizedStrings.Str2869Params.Put(field.DisplayName, field.Number, cells.Length));

							try
							{
								if (field.Number == -1)
								{
									if (field.IsRequired)
										field.ApplyDefaultValue(instance);
								}
								else
									field.ApplyFileValue(instance, cells[field.Number]);
							}
							catch (Exception ex)
							{
								throw new InvalidOperationException(LocalizedStrings.CsvImportError.Put(lineIndex, field.Number, field.Number != -1 ? cells[field.Number] : string.Empty, field.DisplayName), ex);
							}
						}

						if (secMsg == null)
						{
							var execMsg = instance as ExecutionMessage;

							if (execMsg != null)
								execMsg.ExecutionType = ExecutionType;

							buffer.Add(instance);

							if (buffer.Count > 1000)
								FlushBuffer(buffer, drive);
						}
						else
						{
							var security = secMsg.ToSecurity();
							security.ExtensionInfo = new Dictionary<object, object>();

							_entityRegistry.Securities.Save(security);
						}

						_logManager.Application.AddInfoLog(LocalizedStrings.Str2870Params.Put((object)instance, DataType.Name));

						var percent = (int)(((double)reader.BaseStream.Position / len) * 100 - 1).Round();

						if (percent <= prevPercent)
							continue;

						prevPercent = percent;
						_worker.ReportProgress(prevPercent);
					}
				}
			});

			if (buffer.Count > 0)
				FlushBuffer(buffer, drive);

			_worker.ReportProgress(100);
		}

		private Security InitSecurity(SecurityId securityId)
		{
			var id = securityId.ToStringId();
			var security = _entityRegistry.Securities.ReadById(id);

			if (security != null)
				return security;

			security = new Security
			{
				Id = id,
				ExtensionInfo = new Dictionary<object, object>(),
				Code = securityId.SecurityCode,
				Board = ExchangeBoard.GetOrCreateBoard(securityId.SecurityCode),
				Type = securityId.SecurityType,
			};

			_entityRegistry.Securities.Save(security);
			_logManager.Application.AddInfoLog(LocalizedStrings.Str2871Params.Put(id));

			return security;
		}

		private void FlushBuffer(List<dynamic> buffer, IMarketDataDrive drive)
		{
			var registry = ConfigManager.GetService<IStorageRegistry>();

			if (DataType == typeof(NewsMessage))
			{
				registry.GetNewsMessageStorage(drive, _settings.Format).Save(buffer);
			}
			else
			{
				foreach (var typeGroup in buffer.GroupBy(i => i.GetType()))
				{
					var dataType = (Type)typeGroup.Key;

					foreach (var secGroup in typeGroup.GroupBy(i => (SecurityId)i.SecurityId))
					{
						var secId = secGroup.Key;
						var security = InitSecurity(secGroup.Key);

						if (dataType.IsCandleMessage())
						{
							var timeFrame = (TimeSpan)_settings.CandleSettings.Arg;
							var candles = secGroup.Cast<CandleMessage>().ToArray();

							foreach (var candle in candles)
							{
								if (candle.CloseTime < candle.OpenTime)
								{
									// если в файле время закрытия отсутствует
									if (candle.CloseTime.Date == candle.CloseTime)
										candle.CloseTime = default(DateTimeOffset);
								}
								else if (candle.CloseTime > candle.OpenTime)
								{
									// если в файле время открытия отсутствует
									if (candle.OpenTime.Date == candle.OpenTime)
									{
										candle.OpenTime = candle.CloseTime;

										//var tfCandle = candle as TimeFrameCandle;

										//if (tfCandle != null)
										candle.CloseTime += timeFrame;
									}
								}
							}

							registry
								.GetCandleMessageStorage(dataType, security, timeFrame, drive, _settings.Format)
								.Save(candles.OrderBy(c => c.OpenTime));
						}
						else if (dataType == typeof(TimeQuoteChange))
						{
							registry
								.GetQuoteMessageStorage(security, drive, _settings.Format)
								.Save(secGroup
									.GroupBy(i => i.Time)
									.Select(g => new QuoteChangeMessage
									{
										SecurityId = secId,
										ServerTime = g.Key,
										Bids = g.Cast<QuoteChange>().Where(q => q.Side == Sides.Buy).ToArray(),
										Asks = g.Cast<QuoteChange>().Where(q => q.Side == Sides.Sell).ToArray(),
									})
									.OrderBy(md => md.ServerTime));
						}
						else
						{
							var storage = registry.GetStorage(security, dataType, ExecutionType, drive, _settings.Format);

							if (dataType == typeof(ExecutionMessage))
								((IMarketDataStorage<ExecutionMessage>)storage).Save(secGroup.Cast<ExecutionMessage>().OrderBy(m => m.ServerTime));
							else if (dataType == typeof(Level1ChangeMessage))
								((IMarketDataStorage<Level1ChangeMessage>)storage).Save(secGroup.Cast<Level1ChangeMessage>().OrderBy(m => m.ServerTime));
							else
								throw new NotSupportedException(LocalizedStrings.Str2872Params.Put(dataType.Name));
						}
					}
				}
			}

			buffer.Clear();
		}

		void IDisposable.Dispose()
		{
			_worker.CancelAsync();
		}
	}
}