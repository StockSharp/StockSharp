namespace StockSharp.InteractiveBrokers
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Net;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.InteractiveBrokers.Native;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// Контейнер для сессии.
	/// </summary>
	[DisplayName("Interactive Brokers")]
	[CategoryLoc(LocalizedStrings.Str2119Key)]
	[DescriptionLoc(LocalizedStrings.Str2516Key)]
	[CategoryOrderLoc(LocalizedStrings.Str174Key, 0)]
	[CategoryOrderLoc(LocalizedStrings.Str186Key, 1)]
	[CategoryOrderLoc(LocalizedStrings.LoggingKey, 2)]
	public class InteractiveBrokersSessionHolder : MessageSessionHolder
	{
		/// <summary>
		/// Создать <see cref="InteractiveBrokersSessionHolder"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		public InteractiveBrokersSessionHolder(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			Address = DefaultAddress;
			ServerLogLevel = ServerLogLevels.Detail;
			CreateAssociatedSecurity = true;
			IsTransactionEnabled = true;
			IsMarketDataEnabled = true;
		}

		/// <summary>
		/// Адрес по-умолчанию.
		/// </summary>
		public static readonly EndPoint DefaultAddress = new IPEndPoint(IPAddress.Loopback, 7496);

		/// <summary>
		/// Адрес по-умолчанию.
		/// </summary>
		public static readonly EndPoint DefaultGatewayAddress = new IPEndPoint(IPAddress.Loopback, 4001);

		/// <summary>
		/// Адрес.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.AddressKey)]
		[DescriptionLoc(LocalizedStrings.AddressKey)]
		[PropertyOrder(1)]
		public EndPoint Address { get; set; }

		/// <summary>
		/// Уникальный идентификатор. Используется в случае подключения нескольких клиентов к одному терминалу или gateway.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str361Key)]
		[DescriptionLoc(LocalizedStrings.Str2518Key)]
		[PropertyOrder(2)]
		public int ClientId { get; set; }

		/// <summary>
		/// Использовать ли данные реального времени или "замороженные" на сервере брокера. По-умолчанию используются "замороженные" данные.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str2519Key)]
		[DescriptionLoc(LocalizedStrings.Str2520Key)]
		[PropertyOrder(3)]
		public bool IsRealTimeMarketData { get; set; }

		/// <summary>
		/// Уровень логирования сообщений сервера. По-умолчанию равен <see cref="ServerLogLevels.Detail"/>.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str9Key)]
		[DescriptionLoc(LocalizedStrings.Str2521Key)]
		[PropertyOrder(4)]
		public ServerLogLevels ServerLogLevel { get; set; }

		private IEnumerable<GenericFieldTypes> _fields = Enumerable.Empty<GenericFieldTypes>();

		/// <summary>
		/// Поля маркет-данных, которые будут получаться при подписке на Level1 сообщения.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str2522Key)]
		[DescriptionLoc(LocalizedStrings.Str2523Key)]
		[PropertyOrder(4)]
		public IEnumerable<GenericFieldTypes> Fields
		{
			get { return _fields; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_fields = value;
			}
		}

		private IBSocket _session;

		internal IBSocket Session
		{
			get { return _session; }
			set
			{
				if (_session == value)
					return;

				if (_session != null)
					_session.ProcessResponse -= OnProcessResponse;

				_session = value;

				if (_session != null)
					_session.ProcessResponse += OnProcessResponse;
			}
		}

		// TODO https://www.interactivebrokers.com/en/software/api/apiguide/tables/api_message_codes.htm
		enum NotifyCodes
		{
			OrderDuplicateId = 103,
			OrderFilled = 104,
			OrderNotMatchPrev = 105,
			OrderCannotTransmitId = 106,
			OrderCannotTransmitIncomplete = 107,
			OrderPriceOutOfRange = 109,
			OrderCannotTransmit = 132,
			OrderSubmitFailed = 133,
			SecurityNoDefinition = 200,
			Rejected = 201,
			OrderCancelled = 202,
			OrderVolumeTooSmall = 481,
		}

		private bool OnProcessResponse(IBSocket socket)
		{
			var str = socket.ReadStr(false);

			if (str.IsEmpty())
			{
				socket.AddErrorLog(LocalizedStrings.Str2524);
				return false;
			}

			var message = (ResponseMessages)str.To<int>();

			socket.AddDebugLog("Msg: {0}", message);

			if (message == ResponseMessages.Error)
				return false;

			var version = (ServerVersions)socket.ReadInt();

			switch (message)
			{
				case ResponseMessages.CurrentTime:
				{
					// http://www.interactivebrokers.com/en/software/api/apiguide/java/currenttime.htm

					var time = socket.ReadLongDateTime();
					ProcessTimeShift.SafeInvoke(TimeHelper.Now - time);

					break;
				}
				case ResponseMessages.ErrorMessage:
				{
					if (version < ServerVersions.V2)
					{
						ProcessMarketDataError.SafeInvoke(socket.ReadStr());
					}
					else
					{
						var id = socket.ReadInt();
						var code = socket.ReadInt();
						var msg = socket.ReadStr();

						socket.AddInfoLog(() => msg);

						if (id == -1)
							break;

						switch ((NotifyCodes)code)
						{
							case NotifyCodes.OrderCancelled:
							{
								ProcessOrderCancelled.SafeInvoke(id);
								break;
							}
							case NotifyCodes.OrderCannotTransmit:
							case NotifyCodes.OrderCannotTransmitId:
							case NotifyCodes.OrderCannotTransmitIncomplete:
							case NotifyCodes.OrderDuplicateId:
							case NotifyCodes.OrderFilled:
							case NotifyCodes.OrderNotMatchPrev:
							case NotifyCodes.OrderPriceOutOfRange:
							case NotifyCodes.OrderSubmitFailed:
							case NotifyCodes.OrderVolumeTooSmall:
							case NotifyCodes.Rejected:
							{
								ProcessOrderError.SafeInvoke(id, msg);
								break;
							}
							case NotifyCodes.SecurityNoDefinition:
								ProcessSecurityLookupNoFound.SafeInvoke(id);
								break;
							default:
								ProcessMarketDataError.SafeInvoke(LocalizedStrings.Str2525Params.Put(msg, id, code));
								break;
						}
					}

					break;
				}
				case ResponseMessages.VerifyMessageApi:
				{
					/*int version =*/socket.ReadInt();
					/*var apiData = */socket.ReadStr();

					//eWrapper().verifyMessageAPI(apiData);
					break;
				}
				case ResponseMessages.VerifyCompleted:
				{
					/*int version =*/socket.ReadInt();
					var isSuccessfulStr = socket.ReadStr();
					var isSuccessful = "true".CompareIgnoreCase(isSuccessfulStr);
					/*var errorText = */socket.ReadStr();

					if (isSuccessful)
					{
						throw new NotSupportedException();
						//m_parent.startAPI();
					}

					//eWrapper().verifyCompleted(isSuccessful, errorText);
					break;
				}
				case ResponseMessages.DisplayGroupList:
				{
					/*int version =*/socket.ReadInt();
					/*var reqId = */socket.ReadInt();
					/*var groups = */socket.ReadStr();

					//eWrapper().displayGroupList(reqId, groups);
					break;
				}
				case ResponseMessages.DisplayGroupUpdated:
				{
					/*int version =*/socket.ReadInt();
					/*var reqId = */socket.ReadInt();
					/*var contractInfo = */socket.ReadStr();

					//eWrapper().displayGroupUpdated(reqId, contractInfo);
					break;
				}
				default:
				{
					if (!message.IsDefined())
						return false;

					var action = ProcessResponse;

					if (action == null)
						return false;

					action(socket, message, version);
					break;
				}
			}

			return true;
		}

		internal event Action<TimeSpan> ProcessTimeShift;
		internal event Action<int> ProcessSecurityLookupNoFound;
		internal event Action<int> ProcessOrderCancelled;
		internal event Action<int, string> ProcessOrderError;
		internal event Action<string> ProcessMarketDataError;
		internal event Action<IBSocket, ResponseMessages, ServerVersions> ProcessResponse;

		/// <summary>
		/// Время подключения.
		/// </summary>
		[Browsable(false)]
		public DateTime ConnectedTime { get; internal set; }

		internal bool ExtraAuth { get; set; }

		//internal TimeSpan TimeDiff { get; private set; }

		//internal readonly SynchronizedDictionary<SecurityId, SecurityMessage> Securities = new SynchronizedDictionary<SecurityId, SecurityMessage>();

		/// <summary>
		/// Создать для заявки типа <see cref="OrderTypes.Conditional"/> условие, которое поддерживается подключением.
		/// </summary>
		/// <returns>Условие для заявки. Если подключение не поддерживает заявки типа <see cref="OrderTypes.Conditional"/>, то будет возвращено null.</returns>
		public override OrderCondition CreateOrderCondition()
		{
			return new IBOrderCondition();
		}

		/// <summary>
		/// Создать транзакционный адаптер.
		/// </summary>
		/// <returns>Транзакционный адаптер.</returns>
		public override IMessageAdapter CreateTransactionAdapter()
		{
			return new InteractiveBrokersMessageAdapter(MessageAdapterTypes.Transaction, this);
		}

		/// <summary>
		/// Создать адаптер маркет-данных.
		/// </summary>
		/// <returns>Адаптер маркет-данных.</returns>
		public override IMessageAdapter CreateMarketDataAdapter()
		{
			return new InteractiveBrokersMessageAdapter(MessageAdapterTypes.MarketData, this);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Address = storage.GetValue<EndPoint>("Address");
			ClientId = storage.GetValue<int>("ClientId");
			IsRealTimeMarketData = storage.GetValue<bool>("IsRealTimeMarketData");
			ServerLogLevel = storage.GetValue<ServerLogLevels>("ServerLogLevel");
			Fields = storage.GetValue<string>("Fields").Split(",").Select(n => n.To<GenericFieldTypes>()).ToArray();
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("Address", Address.To<string>());
			storage.SetValue("ClientId", ClientId);
			storage.SetValue("IsRealTimeMarketData", IsRealTimeMarketData);
			storage.SetValue("ServerLogLevel", ServerLogLevel.To<string>());
			storage.SetValue("Fields", Fields.Select(t => t.To<string>()).Join(","));
		}

		/// <summary>
		/// Получить строковое представление контейнера.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return LocalizedStrings.Str2526Params.Put(Address);
		}
	}
}