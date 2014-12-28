namespace StockSharp.Hydra.Core
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.History;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Базовый источник, работающий через <see cref="MarketDataConnector{TConnector}"/>.
	/// </summary>
	/// <typeparam name="TConnector">Тип подключения.</typeparam>
	public abstract class ConnectorHydraTask<TConnector> : BaseHydraTask, ISecurityDownloader
		where TConnector : class, IConnector
	{
		private HydraTaskSecurity _allSecurity;
		private readonly SynchronizedDictionary<Security, HydraTaskSecurity> _securityMap = new SynchronizedDictionary<Security, HydraTaskSecurity>();
		private readonly SynchronizedDictionary<string, HydraTaskSecurity> _associatedSecurityCodes = new SynchronizedDictionary<string, HydraTaskSecurity>(StringComparer.InvariantCultureIgnoreCase);

		private static readonly bool _isExternalCandleSource = typeof(IExternalCandleSource).IsAssignableFrom(typeof(TConnector));

		/// <summary>
		/// Инициализировать <see cref="ConnectorHydraTask{TConnector}"/>.
		/// </summary>
		protected ConnectorHydraTask()
		{
			_supportedMarketDataTypes = new[] { typeof(MarketDepth), typeof(Trade), typeof(Level1ChangeMessage) };

			if (_isExternalCandleSource)
				_supportedMarketDataTypes = _supportedMarketDataTypes.Concat(typeof(Candle)).ToArray();
		}

		/// <summary>
		/// Тип задачи.
		/// </summary>
		public override TaskTypes Type
		{
			get { return TaskTypes.Source; }
		}

		/// <summary>
		/// Обертка над подключением <see cref="IConnector"/> для получения маркет-данных в реальном времени.
		/// </summary>
		public MarketDataConnector<TConnector> Connector { get; private set; }

		private readonly Type[] _supportedMarketDataTypes;

		/// <summary>
		/// Поддерживаемые маркет-данные.
		/// </summary>
		public override IEnumerable<Type> SupportedMarketDataTypes
		{
			get { return _supportedMarketDataTypes; }
		}

		/// <summary>
		/// Применить настройки.
		/// </summary>
		/// <param name="settings">Настройки.</param>
		protected override void ApplySettings(HydraTaskSettings settings)
		{
			Connector = CreateTrader(settings);
			Connector.NewSecurities += securities =>
			{
				foreach (var security in securities)
				{
					SaveSecurity(security);

					if (_allSecurity != null)
						SubscribeSecurity(security);
				}
			};

			Connector.ExportStarted += () =>
			{
				if (_allSecurity == null)
					_securityMap.Keys.ForEach(SubscribeSecurity);

				RaiseStarted();
			};
		}

		/// <summary>
		/// Запустить загрузку данных.
		/// </summary>
		protected override void OnStarting()
		{
			// если фильтр по инструментам выключен (выбран инструмент все инструменты)
			_allSecurity = this.GetAllSecurity();

			_securityMap.Clear();
			_associatedSecurityCodes.Clear();

			if (_allSecurity == null)
			{
				_securityMap.AddRange(Settings.Securities.ToDictionary(s => s.Security, s => s));

				var associatedSecurities = Settings
					.Securities
					.Where(p => p.Security.Board == ExchangeBoard.Associated)
					.DistinctBy(sec => sec.Security.Code);

				_associatedSecurityCodes.AddRange(associatedSecurities.ToDictionary(s => s.Security.Code, s => s));
			}

			Connector.Start();
		}

		/// <summary>
		/// Остановить загрузку данных.
		/// </summary>
		protected override void OnStopped()
		{
			Connector.Stop();

			// обрабатка данных, которые могли успеть прийти в момент остановки подключения
			ProcessNewData();

			base.OnStopped();
		}

		/// <summary>
		/// Подписаться на получение реалтайм данных для инструмента.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		protected virtual void SubscribeSecurity(Security security)
		{
			if (_allSecurity == null && !_securityMap.ContainsKey(security))
				return;

			Connector.Connector.RegisterSecurity(security);

			if (CheckSecurity<MarketDepth>(security))
				Connector.Connector.RegisterMarketDepth(security);

			if (CheckSecurity<Trade>(security))
				Connector.Connector.RegisterTrades(security);

			if (CheckSecurity<OrderLogItem>(security))
				Connector.Connector.RegisterOrderLog(security);

			if (CheckSecurity<Level1ChangeMessage>(security))
				Connector.Connector.RegisterSecurity(security);

			if (_isExternalCandleSource)
			{
				var map = _securityMap.TryGetValue(security);

				if (map == null)
					return;

				var source = (IExternalCandleSource)Connector.Connector;

				foreach (var series in map.CandleSeries)
				{
					source.SubscribeCandles(new CandleSeries(series.CandleType, security, series.Arg),
						DateTimeOffset.MinValue, DateTimeOffset.MaxValue);
				}
			}
		}

		private bool CheckSecurity<T>(Security security)
		{
			if (_allSecurity != null)
				return _allSecurity.MarketDataTypesSet.Contains(typeof(T));

			if (security.Board == ExchangeBoard.Associated)
				return false;

			var map = _securityMap.TryGetValue(security);

			if (map != null)
				return map.MarketDataTypesSet.Contains(typeof(T));

			var associatedMap = _associatedSecurityCodes.TryGetValue(security.Code);

			return associatedMap != null && associatedMap.MarketDataTypesSet.Contains(typeof(T));
		}

		void ISecurityDownloader.Refresh(ISecurityStorage storage, Security criteria, Action<Security> newSecurity, Func<bool> isCancelled)
		{
			Connector.Refresh(storage, criteria, newSecurity, isCancelled);
		}

		/// <summary>
		/// Выполнить задачу.
		/// </summary>
		/// <returns>Минимальный интервал, после окончания которого необходимо снова выполнить задачу.</returns>
		protected override TimeSpan OnProcess()
		{
			ProcessNewData();
			return base.OnProcess();
		}

		private void SaveValues<T>(Func<IDictionary<Security, IEnumerable<T>>> getNewValues, Action<Security, IEnumerable<T>> saveValues)
		{
			foreach (var pair in getNewValues().Where(pair => CheckSecurity<T>(pair.Key)))
			{
				saveValues(pair.Key, pair.Value);
			}
		}

		private void ProcessNewData()
		{
			SaveValues(Connector.GetTrades, SaveTrades);
			SaveValues(Connector.GetMarketDepths, SaveDepths);
			SaveValues(Connector.GetOrderLog, SaveOrderLog);
			SaveValues(Connector.GetLevel1Messages, SaveLevel1Changes);
			SaveValues(Connector.GetCandles, SaveCandles);

			SaveNews(Connector.GetNews());
		}

		/// <summary>
		/// Создать подключение к торговой системе.
		/// </summary>
		/// <param name="settings">Настройки.</param>
		/// <returns>Подключение к торговой системе.</returns>
		protected abstract MarketDataConnector<TConnector> CreateTrader(HydraTaskSettings settings);

		internal void InitTrader(Connector connector)
		{
			connector.Parent = this;
		}

		internal void UnInitTrader(TConnector connector)
		{
			connector.Parent = null;
		}
	}
}