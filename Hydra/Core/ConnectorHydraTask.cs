#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Core.CorePublic
File: ConnectorHydraTask.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Core
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.Algo.History;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Базовый источник, работающий через <see cref="IMessageAdapter"/>.
	/// </summary>
	/// <typeparam name="TMessageAdapter">Тип подключения.</typeparam>
	public abstract class ConnectorHydraTask<TMessageAdapter> : BaseHydraTask, ISecurityDownloader
		where TMessageAdapter : IMessageAdapter
	{
		private sealed class StorageEntityFactory : Algo.EntityFactory
		{
			private readonly ISecurityProvider _securityProvider;

			public StorageEntityFactory(ISecurityProvider securityProvider)
			{
				if (securityProvider == null)
					throw new ArgumentNullException(nameof(securityProvider));

				_securityProvider = securityProvider;
			}

			public override Security CreateSecurity(string id)
			{
				return _securityProvider.LookupById(id) ?? base.CreateSecurity(id);
			}
		}

		private HydraTaskSecurity _allSecurity;
		private readonly SynchronizedDictionary<Security, HydraTaskSecurity> _securityMap = new SynchronizedDictionary<Security, HydraTaskSecurity>();
		private readonly SynchronizedDictionary<string, HydraTaskSecurity> _associatedSecurityCodes = new SynchronizedDictionary<string, HydraTaskSecurity>(StringComparer.InvariantCultureIgnoreCase);
		private BufferMessageAdapter _adapter;

		private bool _exportStarted;
		private Security _criteria;
		private Action<Security> _newSecurity;
		private bool _isRefreshed;
		private readonly SyncObject _refreshSync = new SyncObject();
		private bool _wasConnected;

		/// <summary>
		/// Инициализировать <see cref="ConnectorHydraTask{TConnector}"/>.
		/// </summary>
		protected ConnectorHydraTask()
			: this(new Connector())
		{
			
		}

		/// <summary>
		/// Инициализировать <see cref="ConnectorHydraTask{TConnector}"/>.
		/// </summary>
		/// <param name="connector">Подключение к торговой системе.</param>
		protected ConnectorHydraTask(Connector connector)
		{
			if (connector == null)
				throw new ArgumentNullException(nameof(connector));

			Connector = connector;
			Connector.Parent = this;
			Connector.EntityFactory = new StorageEntityFactory(EntityRegistry.Securities);

			Connector.Connected += OnConnected;
			Connector.ConnectionError += OnConnectionError;
			Connector.Disconnected += OnDisconnected;
			Connector.NewSecurities += OnNewSecurities;
		}

		/// <summary>
		/// Подключение к торговой системе.
		/// </summary>
		protected Connector Connector { get; }

		/// <summary>
		/// Адаптер к торговой системе.
		/// </summary>
		protected TMessageAdapter Adapter { get; private set; }

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			Connector.Connected -= OnConnected;
			Connector.ConnectionError -= OnConnectionError;
			Connector.Disconnected -= OnDisconnected;
			Connector.NewSecurities -= OnNewSecurities;

			base.DisposeManaged();
		}

		private void OnNewSecurities(IEnumerable<Security> securities)
		{
			foreach (var security in securities)
			{
				SaveSecurity(security);

				if (_allSecurity != null)
					SubscribeSecurity(security);
			}
		}

		private void OnDisconnected()
		{
			
		}

		private void OnConnectionError(Exception error)
		{
			Stop();
		}

		private void OnConnected()
		{
			if (_allSecurity == null)
				_securityMap.Keys.ForEach(SubscribeSecurity);

			RaiseStarted();

			var connectorSettings = (ConnectorHydraTaskSettings)Settings;

			if (connectorSettings != null && connectorSettings.IsDownloadNews)
				Connector.RegisterNews();
		}

		/// <summary>
		/// Поддерживаемые типы данных.
		/// </summary>
		public override IEnumerable<DataType> SupportedDataTypes { get; } = new[]
		{
			DataType.Create(typeof(QuoteChangeMessage), null),
			DataType.Create(typeof(Level1ChangeMessage), null),
			DataType.Create(typeof(ExecutionMessage), ExecutionTypes.Tick)
		};

		/// <summary>
		/// Запустить загрузку данных.
		/// </summary>
		protected override void OnStarting()
		{
			var connectorSettings = (ConnectorHydraTaskSettings)Settings;

			var settings = Connector.ReConnectionSettings;

			if (connectorSettings == null)
			{
				settings.AttemptCount = -1;
				settings.ReAttemptCount = -1;
			}
			else
			{
				settings.Load(connectorSettings.ReConnectionSettings.Save());

				Connector.LogLevel = LogLevel;
			}

			Adapter = GetAdapter(Connector.TransactionIdGenerator);
			_adapter = new BufferMessageAdapter(Adapter);

			Connector.Adapter.InnerAdapters.Clear();
			Connector.Adapter.InnerAdapters.Add(_adapter);

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

			_adapter.SendInMessage(new ConnectMessage());
		}

		/// <summary>
		/// Остановить загрузку данных.
		/// </summary>
		protected override void OnStopped()
		{
			Connector.Disconnect();

			_criteria = null;
			//_isRefreshed = false;
			_wasConnected = false;
			_exportStarted = false;

			lock (_refreshSync)
			{
				_isRefreshed = true;
				_refreshSync.Pulse();
			}

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

			Connector.RegisterSecurity(security);

			foreach (var tuple in GetDataTypes(security))
			{
				var msgType = tuple.MessageType;

                if (msgType == typeof(QuoteChangeMessage))
					Connector.RegisterMarketDepth(security);
				//else if (msgType == typeof(Level1ChangeMessage))
				//	Connector.RegisterSecurity(security);
				else if (msgType == typeof(ExecutionMessage))
				{
					switch ((ExecutionTypes)tuple.Arg)
					{
						case ExecutionTypes.Tick:
							Connector.RegisterTrades(security);
							break;
						case ExecutionTypes.Order:
						case ExecutionTypes.Trade:
							break;
						case ExecutionTypes.OrderLog:
							Connector.RegisterOrderLog(security);
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}
				else if (msgType.IsCandleMessage())
				{
					var map = _securityMap.TryGetValue(security);

					if (map == null)
						continue;

					_adapter.SendInMessage(new MarketDataMessage
					{
						IsSubscribe = true,
						SecurityId = security.ToSecurityId(),
						DataType = msgType.ToCandleMarketDataType(),
						Arg = tuple.Arg,
						To = DateTimeOffset.MaxValue,
						TransactionId = Connector.TransactionIdGenerator.GetNextId()
					});
				}
			}
		}

		private IEnumerable<DataType> GetDataTypes(Security security)
		{
			if (_allSecurity != null)
				return _allSecurity.DataTypesSet;

			if (security.Board == ExchangeBoard.Associated)
				return Enumerable.Empty<DataType>();

			var map = _securityMap.TryGetValue(security);

			if (map != null)
				return map.DataTypesSet;

			var associatedMap = _associatedSecurityCodes.TryGetValue(security.Code);

			return associatedMap?.DataTypesSet ?? Enumerable.Empty<DataType>();
		}

		void ISecurityDownloader.Refresh(ISecurityStorage storage, Security criteria, Action<Security> newSecurity, Func<bool> isCancelled)
		{
			lock (_refreshSync)
				_isRefreshed = false;

			if (Connector == null)
			{
				_criteria = criteria;
				_newSecurity = newSecurity;
				_wasConnected = false;

				Start();

				lock (_refreshSync)
				{
					if (!_isRefreshed)
						_refreshSync.Wait(TimeSpan.FromMinutes(1));
				}
			}
			else
				ProcessLookupSecurities(criteria, newSecurity, true);
		}

		private void ProcessLookupSecurities(Security criteria, Action<Security> newSecurity, bool wasConnected)
		{
			Action<IEnumerable<Security>> lookupSecuritiesResultHandler = securities =>
			{
				securities.ForEach(newSecurity);

				lock (_refreshSync)
				{
					_isRefreshed = true;
					_refreshSync.Pulse();
				}

				if (!wasConnected)
					Stop();
			};

			Connector.LookupSecuritiesResult += lookupSecuritiesResultHandler;

			try
			{
				Connector.LookupSecurities(criteria);
			}
			catch
			{
				Connector.LookupSecuritiesResult -= lookupSecuritiesResultHandler;
				throw;
			}
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

		private void SaveValues<T>(IDictionary<SecurityId, IEnumerable<T>> newValues, Action<Security, IEnumerable<T>> saveValues)
		{
			if (newValues == null)
				throw new ArgumentNullException(nameof(newValues));

			foreach (var pair in newValues)
				saveValues(GetSecurity(pair.Key), pair.Value);
		}

		private void ProcessNewData()
		{
			SaveValues(_adapter.GetTicks(), SaveTicks);
			SaveValues(_adapter.GetOrderBooks(), SaveDepths);
			SaveValues(_adapter.GetOrderLog(), SaveOrderLog);
			SaveValues(_adapter.GetLevel1(), SaveLevel1Changes);
			SaveValues(_adapter.GetTransactions(), SaveTransactions);

			foreach (var tuple in _adapter.GetCandles())
				SaveCandles(GetSecurity(tuple.Key.Item1), tuple.Value);

			SaveNews(_adapter.GetNews());
		}

		/// <summary>
		/// Получить адаптер к торговой системе.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		/// <returns>Адаптер к торговой системе.</returns>
		protected abstract TMessageAdapter GetAdapter(IdGenerator transactionIdGenerator);
	}
}