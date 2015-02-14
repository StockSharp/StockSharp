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

	using Query = System.Tuple<Algo.Storages.IStorageRegistry, BusinessEntities.Security, Algo.Storages.StorageFormats, Algo.Storages.IMarketDataDrive>;

	using StockSharp.Localization;

	/// <summary>
	/// Таблица доступных рыночных данных.
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
					throw new ArgumentNullException("candleKeys");

				Date = date;
				Candles = new Dictionary<string, bool>();
				candleKeys.ForEach(c => Candles[c] = false);
			}

			public DateTime Date { get; set; }

			public int Year
			{
				get { return Date.Year; }
			}

			public int Month
			{
				get { return Date.Month; }
			}

			public int Day
			{
				get { return Date.Day; }
			}

			public bool IsDepth
			{
				get { return _isDepth; }
				set
				{
					_isDepth = value;
					NotifyChanged("IsDepth");
				}
			}

			public bool IsTick
			{
				get { return _isTick; }
				set
				{
					_isTick = value;
					NotifyChanged("IsTick");
				}
			}

			public bool IsOrderLog
			{
				get { return _isOrderLog; }
				set
				{
					_isOrderLog = value;
					NotifyChanged("IsOrderLog");
				}
			}

			public bool IsLevel1
			{
				get { return _isLevel1; }
				set
				{
					_isLevel1 = value;
					NotifyChanged("IsLevel1");
				}
			}

			public Dictionary<string, bool> Candles { get; private set; }
		}

		private readonly Dictionary<string, DataGridColumn> _candleColumns = new Dictionary<string, DataGridColumn>();
		private readonly ObservableCollection<MarketDataEntry> _visibleEntries;

		private readonly SyncObject _syncObject = new SyncObject();
		private Query _query;

		private bool _isFlushing;
		private bool _isChanged;

		/// <summary>
		/// Событие начала загрузки данных.
		/// </summary>
		public event Action DataLoading;

		/// <summary>
		/// Событие окончания загрузки данных.
		/// </summary>
		public event Action DataLoaded;

		/// <summary>
		/// Создать <see cref="MarketDataGrid"/>.
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

			_serializableColumns = Columns.ToArray();
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

		private readonly IList<DataGridColumn> _serializableColumns;

		/// <summary>
		/// Сохраняемые колонки.
		/// </summary>
		protected override IList<DataGridColumn> SerializableColumns
		{
			get { return _serializableColumns; }
		}

		/// <summary>
		/// Обновить таблицу. Выполняется асинхронно.
		/// </summary>
		/// <param name="storageRegistry">Хранилище маркет-данных.</param>
		/// <param name="security">Инструмент.</param>
		/// <param name="format">Формат данных.</param>
		/// <param name="drive">Хранилище.</param>
		public void BeginMakeEntries(IStorageRegistry storageRegistry, Security security, StorageFormats format, IMarketDataDrive drive)
		{
			if (storageRegistry == null)
				throw new ArgumentNullException("storageRegistry");

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
		/// Отменить операцию, запущенная через <see cref="BeginMakeEntries"/>.
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

			var candles = new Dictionary<string, Tuple<Type, object>>();

			lock (_syncObject)
			{
				if (_isChanged)
					return;
			}

			foreach (var tuple in drive.GetCandleTypes(security.ToSecurityId(), format))
			{
				foreach (var arg in tuple.Item2)
				{
					var key = tuple.Item1.Name.Replace("Candle", string.Empty) + " " + arg;
					candles.Add(key, Tuple.Create(tuple.Item1, arg));
				}
			}

			var candleKeys = candles.Keys.ToArray();

			if (candleKeys.Length > 0)
			{
				this.GuiSync(() =>
				{
					foreach (var candle in candleKeys)
					{
						var column = new DataGridTextColumn
						{
							Header = candle,
							Binding = new Binding
							{
								Path = new PropertyPath("Candles[{0}]".Put(candle)),
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
						_candleColumns.Add(candle, column);

						//ApplyFormatRules(column);
					}

					//ApplyFormatRules();
				});
			}

			Add(dict, storageRegistry.GetTradeStorage(security, drive, format), candleKeys, e => e.IsTick = true);
			Add(dict, storageRegistry.GetMarketDepthStorage(security, drive, format), candleKeys, e => e.IsDepth = true);
			Add(dict, storageRegistry.GetLevel1MessageStorage(security, drive, format), candleKeys, e => e.IsLevel1 = true);
			Add(dict, storageRegistry.GetOrderLogStorage(security, drive, format), candleKeys, e => e.IsOrderLog = true);

			foreach (var candle in candleKeys)
			{
				lock (_syncObject)
				{
					if (_isChanged)
						return;
				}

				var tuple = candles[candle];
				Add(dict, storageRegistry.GetCandleStorage(tuple.Item1, security, tuple.Item2, drive, format), candleKeys, e => e.Candles[candle] = true);
			}

			if (dict.Count > 0)
			{
				// добавляем рабочие дни, которые отсутствуют в данных
				var emptyDays = dict.Keys.Min().Range(dict.Keys.Max(), TimeSpan.FromDays(1))
					.Where(d => security.Board.WorkingTime.IsTradeDate(d, true) && !dict.ContainsKey(d));

				foreach (var batch in emptyDays.Batch(10))
				{
					lock (_syncObject)
					{
						if (_isChanged)
							return;
					}

					TryAddEntries(dict, batch, candleKeys, e => {});
				}
			}

			lock (_syncObject)
			{
				if (_isChanged)
					return;
			}

			this.GuiSync(RefreshSort);
		}

		private void Add(IDictionary<DateTime, MarketDataEntry> dict, IMarketDataStorage storage, string[] candleKeys, Action<MarketDataEntry> action)
		{
			lock (_syncObject)
			{
				if (_isChanged)
					return;
			}

			foreach (var batch in storage.Dates.Batch(10))
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