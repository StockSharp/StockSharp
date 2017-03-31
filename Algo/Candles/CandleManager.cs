#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Candles.Algo
File: CandleManager.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Candles
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Configuration;

	using MoreLinq;

	using StockSharp.Algo.Storages;
	using StockSharp.Logging;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The candles manager.
	/// </summary>
	public class CandleManager : BaseLogReceiver, ICandleManager
	{
		private sealed class CandleManagerSourceList : SynchronizedList<ICandleManagerSource>, ICandleManagerSourceList
		{
			private sealed class SourceInfo : Disposable
			{
				private readonly ICandleManagerSource _source;
				private readonly CandleManager _manager;

				public SourceInfo(ICandleManagerSource source, CandleManager manager)
				{
					if (source == null)
						throw new ArgumentNullException(nameof(source));

					_source = source;
					_manager = manager;

					_source.Processing += OnProcessing;
					_source.Error += _manager.RaiseError;
					_source.CandleManager = _manager;
				}

				private void OnProcessing(CandleSeries series, Candle candle)
				{
					//if (candle.Series == null)
					//{
					//	candle.Series = series;
					//	candle.Source = _source;
					//}

					_manager.Container.AddCandle(series, candle);
				}

				protected override void DisposeManaged()
				{
					base.DisposeManaged();

					_source.Processing -= OnProcessing;
					_source.Error -= _manager.RaiseError;
					_source.Dispose();
				}
			}

			private readonly SynchronizedDictionary<ICandleManagerSource, SourceInfo> _info = new SynchronizedDictionary<ICandleManagerSource,SourceInfo>();
			private readonly CandleManager _manager;

			public CandleManagerSourceList(CandleManager manager)
			{
				if (manager == null)
					throw new ArgumentNullException(nameof(manager));

				_manager = manager;
			}

			protected override void OnAdded(ICandleManagerSource item)
			{
				Subscribe(item);
				base.OnAdded(item);
			}

			protected override bool OnRemoving(ICandleManagerSource item)
			{
				UnSubscribe(item);
				return base.OnRemoving(item);
			}

			protected override void OnInserted(int index, ICandleManagerSource item)
			{
				Subscribe(item);
				base.OnInserted(index, item);
			}

			protected override bool OnClearing()
			{
				foreach (var item in this)
					UnSubscribe(item);

				return base.OnClearing();
			}

			private void Subscribe(ICandleManagerSource source)
			{
				_info.Add(source, new SourceInfo(source, _manager));
			}

			private void UnSubscribe(ICandleManagerSource source)
			{
				lock (_info.SyncRoot)
				{
					var info = _info[source];
					info.Dispose();
					_info.Remove(source);
				}
			}
		}

		private sealed class ExternalCandleSource : Disposable, ICandleManagerSource
		{
			private readonly HashSet<CandleSeries> _series = new HashSet<CandleSeries>();
			private readonly IExternalCandleSource _source;

			public ExternalCandleSource(IExternalCandleSource source)
			{
				if (source == null)
					throw new ArgumentNullException(nameof(source));

				_source = source;
				_source.NewCandles += OnNewCandles;
				_source.Stopped += OnStopped;
			}

			public int SpeedPriority => 1;

			public event Action<CandleSeries, Candle> Processing;
			public event Action<CandleSeries> Stopped;

			event Action<Exception> ICandleSource<Candle>.Error
			{
				add { }
				remove { }
			}

			IEnumerable<Range<DateTimeOffset>> ICandleSource<Candle>.GetSupportedRanges(CandleSeries series)
			{
				return _source.GetSupportedRanges(series);
			}

			void ICandleSource<Candle>.Start(CandleSeries series, DateTimeOffset from, DateTimeOffset to)
			{
				_series.Add(series);
                _source.SubscribeCandles(series, from, to);
			}

			void ICandleSource<Candle>.Stop(CandleSeries series)
			{
				_series.Remove(series);
				_source.UnSubscribeCandles(series);
			}

			private void OnNewCandles(CandleSeries series, IEnumerable<Candle> candles)
			{
				if (!_series.Contains(series))
					return;

				foreach (var c in candles)
				{
					var candle = c.Clone();

					candle.State = CandleStates.Active;
					Processing?.Invoke(series, candle);

					candle.State = CandleStates.Finished;
					Processing?.Invoke(series, candle);
				}
			}

			private void OnStopped(CandleSeries series)
			{
				Stopped?.Invoke(series);
			}

			/// <summary>
			/// Release resources.
			/// </summary>
			protected override void DisposeManaged()
			{
				_source.NewCandles -= OnNewCandles;
				_source.Stopped -= OnStopped;

				base.DisposeManaged();
			}

			ICandleManager ICandleManagerSource.CandleManager { get; set; }
		}

		private sealed class ConnectorCandleSource : Disposable, ICandleManagerSource
		{
			private sealed class SeriesInfo
			{
				public CandleSeries Series { get; }

				public MarketDataMessage Message { get; }

				public Candle CurrentCandle { get; set; }

				public SeriesInfo(CandleSeries series, MarketDataMessage message)
				{
					if (series == null)
						throw new ArgumentNullException(nameof(series));

					if (message == null)
						throw new ArgumentNullException(nameof(message));

					Series = series;
					Message = message;
				}
			}

			private readonly SynchronizedDictionary<long, SeriesInfo> _seriesInfos = new SynchronizedDictionary<long, SeriesInfo>();

			private readonly IConnector _connector;

			public ICandleManager CandleManager { get; set; }

			public int SpeedPriority => 1;

			public event Action<CandleSeries, Candle> Processing;

			public event Action<CandleSeries> Stopped;

			public event Action<Exception> Error;

			public ConnectorCandleSource(IConnector connector)
			{
				if (connector == null)
					throw new ArgumentNullException(nameof(connector));

				_connector = connector;
				_connector.NewMessage += OnConnectorNewMessage;
			}

			IEnumerable<Range<DateTimeOffset>> ICandleSource<Candle>.GetSupportedRanges(CandleSeries series)
			{
				yield return new Range<DateTimeOffset>(DateTimeOffset.MinValue, DateTimeOffset.MaxValue);
			}

			void ICandleSource<Candle>.Start(CandleSeries series, DateTimeOffset from, DateTimeOffset to)
			{
				var transactionId = _connector.TransactionIdGenerator.GetNextId();

				var dataType = series
					.CandleType
					.ToCandleMessageType()
					.ToCandleMarketDataType();

				var mdMsg = new MarketDataMessage
				{
					TransactionId = transactionId,
					DataType = dataType,
					Arg = series.Arg,
					IsSubscribe = true,
					From = from,
					To = to,
					BuildCandlesMode = series.BuildCandlesMode,
				};

				mdMsg.FillSecurityInfo(_connector, series.Security);

				_seriesInfos.Add(transactionId, new SeriesInfo(series, mdMsg));
				((Connector)_connector).SendInMessage(mdMsg); // TODO remove cast
			}

			void ICandleSource<Candle>.Stop(CandleSeries series)
			{
				var pair = _seriesInfos.FirstOrDefault(p => p.Value.Series == series);

				if (pair.Value == null)
					return;

				var transactionId = pair.Key;
				var info = pair.Value;

				var dataType = series
					.CandleType
					.ToCandleMessageType()
					.ToCandleMarketDataType();

				var mdMsg = new MarketDataMessage
				{
					TransactionId = _connector.TransactionIdGenerator.GetNextId(),
					OriginalTransactionId = transactionId,
					DataType = dataType,
					Arg = series.Arg,
					IsSubscribe = false,
					BuildCandlesMode = series.BuildCandlesMode,
				};

				mdMsg.FillSecurityInfo(_connector, series.Security);

				_seriesInfos.Remove(transactionId);
				_connector.MarketDataAdapter.SendInMessage(mdMsg);
			}

			/// <summary>
			/// Release resources.
			/// </summary>
			protected override void DisposeManaged()
			{
				_connector.NewMessage -= OnConnectorNewMessage;

				base.DisposeManaged();
			}

			private void OnConnectorNewMessage(Message message)
			{
				switch (message.Type)
				{
					case MessageTypes.CandleTimeFrame:
					case MessageTypes.CandlePnF:
					case MessageTypes.CandleRange:
					case MessageTypes.CandleRenko:
					case MessageTypes.CandleTick:
					case MessageTypes.CandleVolume:
					{
						var candleMsg = (CandleMessage)message;
						var info = _seriesInfos.TryGetValue(candleMsg.OriginalTransactionId);

						if (info != null)
							ProcessCandle(info, candleMsg);

						break;
					}

					case MessageTypes.MarketDataFinished:
					{
						var msg = (MarketDataFinishedMessage)message;
						var info = _seriesInfos.TryGetValue(msg.OriginalTransactionId);

						if (info == null)
							break;

						Stopped?.Invoke(info.Series);
						break;
					}
				}
			}

			private void ProcessCandle(SeriesInfo info, CandleMessage candleMsg)
			{
				if (info.CurrentCandle != null && info.CurrentCandle.OpenTime == candleMsg.OpenTime)
				{
					if (info.CurrentCandle.State == CandleStates.Finished)
						return;

					info.CurrentCandle.Update(candleMsg);
				}
				else
					info.CurrentCandle = candleMsg.ToCandle(info.Series);

				Processing?.Invoke(info.Series, info.CurrentCandle);

				if (candleMsg.IsFinished)
					Stopped?.Invoke(info.Series);
			}
		}

		private readonly SynchronizedDictionary<CandleSeries, CandleSourceEnumerator<ICandleManagerSource, Candle>> _series = new SynchronizedDictionary<CandleSeries, CandleSourceEnumerator<ICandleManagerSource, Candle>>();

		/// <summary>
		/// Initializes a new instance of the <see cref="CandleManager"/>.
		/// </summary>
		public CandleManager()
		{
			Sources = new CandleManagerSourceList(this)
			{
				new StorageCandleSource(),
			};
		}

		///// <summary>
		///// Initializes a new instance of the <see cref="CandleManager"/>.
		///// </summary>
		///// <param name="builderSource">The data source for <see cref="ICandleBuilder"/>.</param>
		//public CandleManager(ICandleBuilderSource builderSource)
		//	: this()
		//{
		//	if (builderSource == null)
		//		throw new ArgumentNullException(nameof(builderSource));

		//	Sources.OfType<ICandleBuilder>().ForEach(b => b.Sources.Add(builderSource));
		//}

		/// <summary>
		/// Initializes a new instance of the <see cref="CandleManager"/>.
		/// </summary>
		/// <param name="connector">The connection to trading system to create the source for tick trades by default.</param>
		public CandleManager(IConnector connector)
			: this()
		{
			Sources.Add(new ConnectorCandleSource(connector));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CandleManager"/>.
		/// </summary>
		/// <param name="candleSource">The external candles source.</param>
		public CandleManager(IExternalCandleSource candleSource)
			: this()
		{
			Sources.Add(new ExternalCandleSource(candleSource));
		}

		private ICandleManagerContainer _container = new CandleManagerContainer();

		/// <summary>
		/// The data container.
		/// </summary>
		public ICandleManagerContainer Container
		{
			get { return _container; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				if (value == _container)
					return;

				_container.Dispose();
				_container = value;
			}
		}

		private IStorageRegistry _storageRegistry;

		/// <summary>
		/// The data storage. To be sent to all sources that implement the interface <see cref="IStorageCandleSource"/>.
		/// </summary>
		public IStorageRegistry StorageRegistry
		{
			get { return _storageRegistry; }
			set
			{
				_storageRegistry = value;
				Sources.OfType<IStorageCandleSource>().ForEach(s => s.StorageRegistry = value);
			}
		}

		/// <summary>
		/// All currently active candles series started via <see cref="Start"/>.
		/// </summary>
		public IEnumerable<CandleSeries> Series
		{
			get { return _series.SyncGet(d => d.Keys.ToArray()); }
		}

		/// <summary>
		/// Candles sources.
		/// </summary>
		public ICandleManagerSourceList Sources { get; }

		/// <summary>
		/// The source priority by speed (0 - the best).
		/// </summary>
		public int SpeedPriority
		{
			get { throw new NotSupportedException(); }
		}

		/// <summary>
		/// A new value for processing occurrence event.
		/// </summary>
		public event Action<CandleSeries, Candle> Processing;

		/// <summary>
		/// The series processing end event.
		/// </summary>
		public event Action<CandleSeries> Stopped;

		/// <summary>
		/// The candles creating error event.
		/// </summary>
		public event Action<Exception> Error;

		/// <summary>
		/// To get time ranges for which this source of passed candles series has data.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <returns>Time ranges.</returns>
		public virtual IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			return Sources.SelectMany(s => s.GetSupportedRanges(series)).JoinRanges().ToArray();
		}

		/// <summary>
		/// To send data request.
		/// </summary>
		/// <param name="series">The candles series for which data receiving should be started.</param>
		/// <param name="from">The initial date from which you need to get data.</param>
		/// <param name="to">The final date by which you need to get data.</param>
		public virtual void Start(CandleSeries series, DateTimeOffset from, DateTimeOffset to)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			CandleSourceEnumerator<ICandleManagerSource, Candle> enumerator;

			lock (_series.SyncRoot)
			{
				if (_series.ContainsKey(series))
					throw new ArgumentException(LocalizedStrings.Str650Params.Put(series), nameof(series));

				enumerator = new CandleSourceEnumerator<ICandleManagerSource, Candle>(series, from, to,
					series.Security is IndexSecurity ? (IEnumerable<ICandleManagerSource>)new[] { new IndexSecurityCandleManagerSource(this, ConfigManager.GetService<ISecurityProvider>(), from, to) } : Sources,
					c =>
					{
						Processing?.Invoke(series, c);
						return c.OpenTime;
					},
					() =>
					{
						//Stop(series);
						_series.Remove(series);
						Stopped?.Invoke(series);
					});

				_series.Add(series, enumerator);

				//series.CandleManager = this;
				series.From = from;
				series.To = to;

				Container.Start(series, from, to);
			}
			
			enumerator.Start();
		}

		/// <summary>
		/// To stop data receiving starting through <see cref="Start"/>.
		/// </summary>
		/// <param name="series">Candles series.</param>
		public virtual void Stop(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			var enumerator = _series.TryGetValue(series);

			if (enumerator == null)
				return;

			enumerator.Stop();
		}

		/// <summary>
		/// To call the event <see cref="CandleManager.Error"/>.
		/// </summary>
		/// <param name="error">Error info.</param>
		protected virtual void RaiseError(Exception error)
		{
			Error?.Invoke(error);
			this.AddErrorLog(error);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			lock (Sources.SyncRoot)
			{
				Sources.ForEach(s => s.Dispose());
				Sources.Clear();
			}

			Container.Dispose();
			base.DisposeManaged();
		}
	}
}