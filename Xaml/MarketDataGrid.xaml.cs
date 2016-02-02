#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: MarketDataGrid.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Globalization;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Data;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Messages;

	using Query = System.Tuple<Algo.Storages.IStorageRegistry, BusinessEntities.Security, Algo.Storages.StorageFormats, Algo.Storages.IMarketDataDrive>;

	/// <summary>
	/// The table of available market data.
	/// </summary>
	public partial class MarketDataGrid
	{
		private sealed class MarketDataEntry : NotifiableObject
		{
			private bool _isDepth;
			private bool _isTick;
			private bool _isOrderLog;
			private bool _isLevel1;

			public MarketDataEntry(DateTime date, IEnumerable<string> candleKeys)
			{
				if (candleKeys == null)
					throw new ArgumentNullException(nameof(candleKeys));

				Date = date;
				Candles = new Dictionary<string, bool>();
				candleKeys.ForEach(c => Candles[c] = false);
			}

			public DateTime Date { get; }

			public int Year => Date.Year;

			public int Month => Date.Month;

			public int Day => Date.Day;

			public bool IsDepth
			{
				get { return _isDepth; }
				set
				{
					_isDepth = value;
					NotifyChanged(nameof(IsDepth));
				}
			}

			public bool IsTick
			{
				get { return _isTick; }
				set
				{
					_isTick = value;
					NotifyChanged(nameof(IsTick));
				}
			}

			public bool IsOrderLog
			{
				get { return _isOrderLog; }
				set
				{
					_isOrderLog = value;
					NotifyChanged(nameof(IsOrderLog));
				}
			}

			public bool IsLevel1
			{
				get { return _isLevel1; }
				set
				{
					_isLevel1 = value;
					NotifyChanged(nameof(IsLevel1));
				}
			}

			public Dictionary<string, bool> Candles { get; }
		}

		private readonly Dictionary<string, DataGridColumn> _candleColumns = new Dictionary<string, DataGridColumn>();
		private readonly ObservableCollection<MarketDataEntry> _visibleEntries;

		private readonly SyncObject _syncObject = new SyncObject();
		private Query _query;

		private bool _isFlushing;
		private bool _isChanged;

		/// <summary>
		/// The data loading start event.
		/// </summary>
		public event Action DataLoading;

		/// <summary>
		/// The data loading end event.
		/// </summary>
		public event Action DataLoaded;

		/// <summary>
		/// Initializes a new instance of the <see cref="MarketDataGrid"/>.
		/// </summary>
		public MarketDataGrid()
		{
			InitializeComponent();

			ItemsSource = _visibleEntries = new ObservableCollection<MarketDataEntry>();

			//for (var i = 3; i < Columns.Count; i++)
			//	ApplyFormatRules(Columns[i]);

			//ApplyFormatRules();

			ShowHeaderInGroupTitle = false;
			GroupingColumnConverters.Add("Month", (IValueConverter)Resources["monthToNameConverter"]);

			GroupingColumns.Add(Columns[0]);
			GroupingColumns.Add(Columns[1]);

			SerializableColumns = Columns.ToArray();
		}

		//private void ApplyFormatRules(DataGridColumn column)
		//{
		//	FormatRules.Add(column, new FormatRule
		//	{
		//		Value = true,
		//		Condition = ComparisonOperator.Equal,
		//		Background = Brushes.LightGreen,
		//	});

		//	FormatRules.Add(column, new FormatRule
		//	{
		//		Value = false,
		//		Condition = ComparisonOperator.Equal,
		//		Background = Brushes.Pink,
		//	});
		//}

		/// <summary>
		/// Saved columns.
		/// </summary>
		protected override IList<DataGridColumn> SerializableColumns { get; }

		/// <summary>
		/// To refresh the table. It is carried out asynchronously.
		/// </summary>
		/// <param name="storageRegistry">Market-data storage.</param>
		/// <param name="security">Security.</param>
		/// <param name="format">Data format.</param>
		/// <param name="drive">Storage.</param>
		public void BeginMakeEntries(IStorageRegistry storageRegistry, Security security, StorageFormats format, IMarketDataDrive drive)
		{
			if (storageRegistry == null)
				throw new ArgumentNullException(nameof(storageRegistry));

			lock (_syncObject)
			{
				_query = new Query(storageRegistry, security, format, drive);
				_isChanged = true;

				if (_isFlushing)
					return;

				_isFlushing = true;

				ThreadingHelper
					.Thread(() => CultureInfo.InvariantCulture.DoInCulture(OnFlush))
					.Launch();
			}
		}

		/// <summary>
		/// To cancel the operation launched by <see cref="BeginMakeEntries"/>.
		/// </summary>
		public void CancelMakeEntires()
		{
			lock (_syncObject)
			{
				if (!_isFlushing)
					return;

				_query = null;
				_isChanged = true;
			}
		}

		private void OnFlush()
		{
			try
			{
				this.GuiSync(() => DataLoading.SafeInvoke());

				while (true)
				{
					Query query;

					lock (_syncObject)
					{
						_isChanged = false;

						if (_query == null)
						{
							this.GuiAsync(() => DataLoaded.SafeInvoke());

							_isFlushing = false;
							break;
						}

						query = _query;
						_query = null;
					}

					Process(query.Item1, query.Item2, query.Item3, query.Item4);
				}
			}
			catch (Exception ex)
			{
				this.GuiAsync(() =>
				{
					DataLoaded.SafeInvoke();
					throw new InvalidOperationException(LocalizedStrings.Str1538, ex);
				});
			}
		}

		private void Process(IStorageRegistry storageRegistry, Security security, StorageFormats format, IMarketDataDrive drive)
		{
			this.GuiSync(() =>
			{
				_visibleEntries.Clear();
			
				Columns.RemoveRange(_candleColumns.Values);
				_candleColumns.Values.ForEach(c => FormatRules.Remove(c));
				_candleColumns.Clear();
			});

			if (security == null)
				return;

			var dict = new Dictionary<DateTime, MarketDataEntry>();

			drive = drive ?? storageRegistry.DefaultDrive;

			var candles = new Dictionary<string, DataType>();

			lock (_syncObject)
			{
				if (_isChanged)
					return;
			}

			foreach (var tuple in drive.GetAvailableDataTypes(security.ToSecurityId(), format))
			{
				if (!tuple.MessageType.IsCandleMessage())
					continue;

				var key = tuple.MessageType.Name.Replace("CandleMessage", string.Empty) + " " + tuple.Arg;
				candles.Add(key, tuple.Clone());
			}

			var candleNames = candles.Keys.ToArray();

			if (candleNames.Length > 0)
			{
				this.GuiSync(() =>
				{
					foreach (var candleName in candleNames)
					{
						var column = new DataGridTextColumn
						{
							Header = candleName,
							Binding = new Binding
							{
								Path = new PropertyPath("Candles[{0}]".Put(candleName)),
								Converter = new BoolToCheckMarkConverter()
							}
						};
						//var cbElement = new FrameworkElementFactory(typeof(CheckBox));

						//var column = new DataGridTemplateColumn
						//{
						//	Header = candle,
						//	CellStyle = new Style(),
						//	//SortMemberPath = "Candles[{0}]".Put(key)
						//	CellTemplate = new DataTemplate(typeof(CheckBox))
						//	{
						//		VisualTree = cbElement,
						//	}
						//};

						//var bind = new Binding { Path = new PropertyPath("Candles[{0}]".Put(candle)) };
						
						//cbElement.SetBinding(ToggleButton.IsCheckedProperty, bind);
						//cbElement.SetValue(IsHitTestVisibleProperty, false);
						//cbElement.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);

						Columns.Add(column);
						_candleColumns.Add(candleName, column);

						//ApplyFormatRules(column);
					}

					//ApplyFormatRules();
				});
			}

			Add(dict, storageRegistry.GetTickMessageStorage(security, drive, format).Dates, candleNames, e => e.IsTick = true);
			Add(dict, storageRegistry.GetQuoteMessageStorage(security, drive, format).Dates, candleNames, e => e.IsDepth = true);
			Add(dict, storageRegistry.GetLevel1MessageStorage(security, drive, format).Dates, candleNames, e => e.IsLevel1 = true);
			Add(dict, storageRegistry.GetOrderLogMessageStorage(security, drive, format).Dates, candleNames, e => e.IsOrderLog = true);

			foreach (var c in candleNames)
			{
				var candleName = c;

				lock (_syncObject)
				{
					if (_isChanged)
						return;
				}

				var tuple = candles[candleName];
				Add(dict, storageRegistry.GetCandleMessageStorage(tuple.MessageType, security, tuple.Arg, drive, format).Dates, candleNames, e => e.Candles[candleName] = true);
			}

			if (dict.Count > 0)
			{
				// add gaps by working days
				var emptyDays = dict.Keys.Min().Range(dict.Keys.Max(), TimeSpan.FromDays(1))
					.Where(d => !dict.ContainsKey(d)/* && security.Board.WorkingTime.IsTradeDate(d, true)*/)
					.OrderBy()
					.ToList();

				foreach (var monthGroup in emptyDays.ToArray().GroupBy(d => new DateTime(d.Year, d.Month, 1)))
				{
					var firstDay = monthGroup.First();
					var lastDay = monthGroup.Last();

					// whole month do not have any values
					if (firstDay.Day == 1 && lastDay.Day == monthGroup.Key.DaysInMonth())
						emptyDays.RemoveRange(monthGroup);
				}

				Add(dict, emptyDays, candleNames, e => { });
			}

			lock (_syncObject)
			{
				if (_isChanged)
					return;
			}

			this.GuiSync(RefreshSort);
		}

		private void Add(IDictionary<DateTime, MarketDataEntry> dict, IEnumerable<DateTime> dates, string[] candleKeys, Action<MarketDataEntry> action)
		{
			lock (_syncObject)
			{
				if (_isChanged)
					return;
			}

			foreach (var batch in dates.Batch(10))
			{
				lock (_syncObject)
				{
					if (_isChanged)
						return;
				}

				TryAddEntries(dict, batch, candleKeys, action);
			}
		}

		private void TryAddEntries(IDictionary<DateTime, MarketDataEntry> dict, IEnumerable<DateTime> dates, IEnumerable<string> candleKeys, Action<MarketDataEntry> action)
		{
			this.GuiSync(() =>
				dates
					.Select(date => dict.SafeAdd(date, d =>
					{
						var entry = new MarketDataEntry(d, candleKeys);
						_visibleEntries.Add(entry);
						return entry;
					}))
					.ForEach(action));
		}
	}

	sealed class BoolToCheckMarkConverter : IValueConverter
	{
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (bool)value ? "\u2713" : string.Empty;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}