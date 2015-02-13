namespace StockSharp.Quik
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using Ecng.Common;
	using Ecng.Collections;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Типы транзакций.
	/// </summary>
	public enum TransactionTypes
	{
		/// <summary>
		/// Регистрация заявки.
		/// </summary>
		Register,

		/// <summary>
		/// Изменение заявки.
		/// </summary>
		ReRegister,

		/// <summary>
		/// Отмена заявки.
		/// </summary>
		Cancel,

		/// <summary>
		/// Отмена группы заявок.
		/// </summary>
		CancelGroup,
	}

	/// <summary>
	/// Виды действий транзакций.
	/// </summary>
	public static class TransactionActions
	{
		/// <summary>
		/// Новая заявка.
		/// </summary>
		public const string NewOrder = "NEW_ORDER";

		/// <summary>
		/// Новая стоп-заявка.
		/// </summary>
		public const string NewStopOrder = "NEW_STOP_ORDER";

		/// <summary>
		/// Переставить заявки на рынке FORTS.
		/// </summary>
		public const string MoveOrders = "MOVE_ORDERS";

		/// <summary>
		/// Снять стоп-заявку.
		/// </summary>
		public const string KillStopOrder = "KILL_STOP_ORDER";

		/// <summary>
		/// Снять заявку.
		/// </summary>
		public const string KillOrder = "KILL_ORDER";

		/// <summary>
		/// Снять все стоп-заявки.
		/// </summary>
		public const string KillAllStopOrders = "KILL_ALL_STOP_ORDERS";

		/// <summary>
		/// Снять все заявки из торговой системы.
		/// </summary>
		public const string KillAllOrders = "KILL_ALL_ORDERS";

		///<summary>
		/// Снять все заявки на рынке FORTS.
		///</summary>
		public const string KillAllFuturesOrders = "KILL_ALL_FUTURES_ORDERS";

		///<summary>
		/// Новая заявка на внебиржевую сделку.
		///</summary>
		public const string NewNegDeal = "NEW_NEG_DEAL";

		///<summary>
		/// Новая заявка на сделку РЕПО.
		///</summary>
		public const string NewRepoNegDeal = "NEW_REPO_NEG_DEAL";

		///<summary>
		/// Новая заявка на сделку модифицированного РЕПО (РЕПО-М).
		///</summary>
		public const string NewExtRepoNegDeal = "NEW_EXT_REPO_NEG_DEAL";

		///<summary>
		/// Cнять заявку на внебиржевую сделку или заявку на сделку РЕПО.
		///</summary>
		public const string KillNegDeal = "KILL_NEG_DEAL";

		///<summary>
		/// Cнять все заявки на внебиржевые сделки и заявки на сделки РЕПО.
		///</summary>
		public const string KillAllNegDeal = "KILL_ALL_NEG_DEAL";

		/// <summary>
		/// Айсберг заявка.
		/// </summary>
		public const string Iceberg = "Ввод айсберг заявки";
	}

	/// <summary>
	/// Типы стоп-заявки.
	/// </summary>
	public static class TransactionStopOrderKinds
	{
		/// <summary>
		/// Стоп-лимит.
		/// </summary>
		public const string SimpleStopLimit = "SIMPLE_STOP_ORDER";

		/// <summary>
		/// Со связанной заявкой.
		/// </summary>
		public const string WithLinkedLimitOrder = "WITH_LINKED_LIMIT_ORDER";

		/// <summary>
		/// С условием по другой бумаге.
		/// </summary>
		public const string ConditionPriceByOtherSecurity = "CONDITION_PRICE_BY_OTHER_SEC";

		/// <summary>
		/// Тэйк-профит.
		/// </summary>
		public const string TakeProfit = "TAKE_PROFIT_STOP_ORDER";

		/// <summary>
		/// Тэйк-профит и стоп-лимит.
		/// </summary>
		public const string TakeProfitAndStopLimit = "TAKE_PROFIT_AND_STOP_LIMIT_ORDER";

		/// <summary>
		/// По исполнению заявки.
		/// </summary>
		public const string ActivatedByOrder = "ACTIVATED_BY_ORDER_";
	}

	/// <summary>
	/// Лицо, от имени которого и за чей счет регистрируется сделка (параметр внебиржевой сделки).
	/// </summary>
	public static class ForAccountValues
	{
		/// <summary>
		/// От своего имени, за свой счет.
		/// </summary>
		public const string OwnOwn = "OWNOWN";

		/// <summary>
		/// От своего имени, за счет клиента. 
		/// </summary>
		public const string OwnCli = "OWNCLI";

		/// <summary>
		/// От своего имени, за счет доверительного управления.
		/// </summary>
		public const string OwnDup = "OWNDUP";

		/// <summary>
		/// От имени клиента, за счет клиента.
		/// </summary>
		public const string CliCli = "CLICLI";
	}

	/// <summary>
	/// Специальный класс для создания строк транзакций Quik-a.
	/// </summary>
	public sealed class Transaction : Dictionary<string, string>
	{
		/// <summary>
		/// Создать <see cref="Transaction"/>.
		/// </summary>
		/// <param name="transactionType">Тип транзакции.</param>
		/// <param name="message">Сообщение, ассоциированное с данной транзакцией.</param>
		public Transaction(TransactionTypes transactionType, OrderMessage message)
			: base(StringComparer.InvariantCultureIgnoreCase)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			TransactionType = transactionType;
			Message = (OrderMessage)message.Clone();
		}

		/// <summary>
		/// Тип транзакции.
		/// </summary>
		public TransactionTypes TransactionType { get; private set; }

		/// <summary>
		/// Сообщение, ассоциированное с данной транзакцией.
		/// </summary>
		public OrderMessage Message { get; private set; }

		/// <summary>
		/// Код класса, по которому выполняется транзакция.
		/// </summary>
		public const string ClassCode = "CLASSCODE";

		/// <summary>
		/// Код инструмента, по которому выполняется транзакция.
		/// </summary>
		public const string SecurityCode = "SECCODE";

		/// <summary>
		/// Вид транзакции.
		/// </summary>
		public const string Action = "ACTION";

		/// <summary>
		/// Номер счета.
		/// </summary>
		public const string Account = "ACCOUNT";

		/// <summary>
		/// Код клиента.
		/// </summary>
		public const string ClientCode = "CLIENT_CODE";

		/// <summary>
		/// Тип заявки.
		/// </summary>
		public const string Type = "TYPE";

		/// <summary>
		/// Направление заявки.
		/// </summary>
		public const string Side = "OPERATION";

		/// <summary>
		/// Время жизни лимитной заявки.
		/// </summary>
		public const string TimeInForce = "EXECUTION_CONDITION";

		/// <summary>
		/// Количество лотов в заявке.
		/// </summary>
		public const string Volume = "QUANTITY";

		/// <summary>
		/// Видимое количество лотов в заявке.
		/// </summary>
		public const string VisibleVolume = "Видимое количество";

		/// <summary>
		/// Цена заявки, за единицу инструмента.
		/// </summary>
		public const string Price = "PRICE";

		/// <summary>
		/// Стоп-цена, за единицу инструмента.
		/// </summary>
		public const string StopPrice = "STOPPRICE";

		/// <summary>
		/// Тип стоп-заявки.
		/// </summary>
		public const string StopOrderKind = "STOP_ORDER_KIND";

		/// <summary>
		/// Класс инструмента условия.
		/// </summary>
		public const string OtherSecurityClassCode = "STOPPRICE_CLASSCODE";

		/// <summary>
		/// Код инструмента условия.
		/// </summary>
		public const string OtherSecurityCode = "STOPPRICE_SECCODE";

		/// <summary>
		/// Направление предельного изменения стоп-цены.
		/// </summary>
		public const string StopPriceCondition = "STOPPRICE_CONDITION";

		/// <summary>
		/// Цена связанной лимитированной заявки.
		/// </summary>
		public const string LinkedOrderPrice = "LINKED_ORDER_PRICE";

		/// <summary>
		/// Срок действия стоп-заявки.
		/// </summary>
		public const string ExpiryDate = "EXPIRY_DATE";

		/// <summary>
		/// Цена условия «стоп-лимит» для заявки типа «Тэйк-профит и стоп-лимит».
		/// </summary>
		public const string StopLimitPrice = "STOPPRICE2";

		/// <summary>
		/// Признак исполнения заявки по рыночной цене при наступлении условия «стоп-лимит».
		/// </summary>
		public const string MarketStopLimit = "MARKET_STOP_LIMIT";

		/// <summary>
		/// Признак исполнения заявки по рыночной цене при наступлении условия «тэйк-профит».
		/// </summary>
		public const string MarketTakeProfit = "MARKET_TAKE_PROFIT";

		/// <summary>
		/// Признак действия заявки типа «Тэйк-профит и стоп-лимит» в течение определенного интервала времени.
		/// </summary>
		public const string IsActiveInTime = "IS_ACTIVE_IN_TIME";

		/// <summary>
		/// Время начала действия заявки типа «Тэйк-профит и стоп-лимит» в формате «ЧЧММСС».
		/// </summary>
		public const string ActiveFrom = "ACTIVE_FROM_TIME";

		/// <summary>
		/// Время окончания действия заявки типа «Тэйк-профит и стоп-лимит» в формате «ЧЧММСС».
		/// </summary>
		public const string ActiveTo = "ACTIVE_TO_TIME";

		/// <summary>
		/// Номер заявки, снимаемой из торговой системы.
		/// </summary>
		public const string OrderId = "ORDER_KEY";

		/// <summary>
		/// Номер стоп-заявки, снимаемой из торговой системы.
		/// </summary>
		public const string StopOrderId = "STOP_ORDER_KEY";

		/// <summary>
		/// Уникальный идентификационный номер заявки.
		/// </summary>
		public const string TransactionId = "TRANS_ID";

		/// <summary>
		/// Текстовый комментарий.
		/// </summary>
		public const string Comment = "COMMENT";

		/// <summary>
		/// Признак снятия стоп-заявки при частичном исполнении связанной лимитированной заявки.
		/// </summary>
		public const string LinkedOrderCancel = "KILL_IF_LINKED_ORDER_PARTLY_FILLED";

		/// <summary>
		/// Величина отступа от максимума (минимума) цены последней сделки.
		/// </summary>
		public const string OffsetValue = "OFFSET";

		/// <summary>
		/// Единицы измерения отступа.
		/// </summary>
		public const string OffsetUnit = "OFFSET_UNITS";

		/// <summary>
		/// Величина защитного спрэда.
		/// </summary>
		public const string SpreadValue = "SPREAD";

		/// <summary>
		/// Единицы измерения защитного спрэда.
		/// </summary>
		public const string SpreadUnit = "SPREAD_UNITS";

		/// <summary>
		/// Регистрационный номер заявки-условия.
		/// </summary>
		public const string ConditionOrderId = "BASE_ORDER_KEY";

		/// <summary>
		/// Признак использования в качестве объема заявки «по исполнению» исполненного количества бумаг заявки-условия.
		/// </summary>
		public const string ConditionOrderUseMatchedBalance = "USE_BASE_ORDER_BALANCE";

		/// <summary>
		/// Признак активации заявки «по исполнению» при частичном исполнении заявки-условия.
		/// </summary>
		public const string ConditionOrderPartiallyMatched = "ACTIVATE_IF_BASE_ORDER_PARTLY_FILLED";

		/// <summary>
		/// Идентификатор базового контракта для фьючерсов или опционов.
		/// </summary>
		public const string BaseContract = "BASE_CONTRACT";

		/// <summary>
		/// Режим перестановки заявок на рынке FORTS.
		/// </summary>
		public const string FortsMode = "MODE";

		/// <summary>
		/// Номер первой заявки.
		/// </summary>
		public const string FirstOrderId = "FIRST_ORDER_NUMBER";

		/// <summary>
		/// Количество в первой заявке.
		/// </summary>
		public const string FirstOrderNewVolume = "FIRST_ORDER_NEW_QUANTITY";

		/// <summary>
		/// Цена в первой заявке.
		/// </summary>
		public const string FirstOrderNewPrice = "FIRST_ORDER_NEW_PRICE";

		/// <summary>
		/// Номер второй заявки.
		/// </summary>
		public const string SecondOrderNumber = "SECOND_ORDER_NUMBER";

		/// <summary>
		/// Количество во второй заявке.
		/// </summary>
		public const string SecondOrderNewVolume = "SECOND_ORDER_NEW_QUANTITY";

		/// <summary>
		/// Цена во второй заявке.
		/// </summary>
		public const string SecondOrderNewPrice = "SECOND_ORDER_NEW_PRICE";

		/// <summary>
		/// Код организации – партнера по внебиржевой сделке.
		/// </summary>
		public const string Partner = "PARTNER";

		/// <summary>
		/// Срок РЕПО. Параметр сделок РЕПО-М.
		/// </summary>
		public const string RepoTerm = "RepoTERM";

		/// <summary>
		/// Ставка РЕПО, в процентах. 
		/// </summary>
		public const string RepoRate = "RepoRATE";

		/// <summary>
		/// Признак блокировки бумаг на время операции РЕПО («YES», «NO»).
		/// </summary>
		public const string BlockSecurities = "BLOCK_SECURITIES";

		/// <summary>
		/// Ставка фиксированного возмещения, выплачиваемого в случае неисполнения второй части РЕПО, в процентах.
		/// </summary>
		public const string RefundRate = "REFUNDRATE";

		/// <summary>
		/// Ссылка, которая связывает две сделки РЕПО или РПС. 
		/// Сделка может быть заключена только между контрагентами, указавшими одинаковое значение этого параметра в своих заявках. 
		/// Параметр представляет собой произвольный набор количеством до 10 символов (допускаются цифры и буквы).	
		/// </summary>
		/// <remarks>
		/// Необязательный параметр.
		/// </remarks>
		public const string MatchRef = "MATCHREF";

		/// <summary>
		/// Код расчетов при исполнении внебиржевых заявок. 
		/// </summary>
		public const string SettleCode = "SETTLE_CODE";

		/// <summary>
		/// Цена второй части РЕПО. 
		/// </summary>
		public const string SecondPrice = "PRICE2";

		/// <summary>
		/// Дата исполнения внебиржевой сделки.
		/// </summary>
		public const string SettleDate = "SETTLE_DATE";

		/// <summary>
		/// Начальное значение дисконта в заявке на сделку РЕПО-М.
		/// </summary>
		public const string StartDiscount = "START_DISCOUNT";

		/// <summary>
		/// Нижнее предельное значение дисконта в заявке на сделку РЕПО-М.
		/// </summary>
		public const string LowerDiscount = "LOWER_DISCOUNT";

		/// <summary>
		/// Верхнее предельное значение дисконта в заявке на сделку РЕПО-М.
		/// </summary>
		public const string UpperDiscount = "UPPER_DISCOUNT";

		/// <summary>
		/// Объем сделки РЕПО-М в рублях.
		/// </summary>
		public const string RepoValue = "RepoVALUE";

		/// <summary>
		/// Код валюты расчетов по внебиржевой сделки, например «SUR» – рубли РФ, «USD» – доллары США. Параметр внебиржевой сделки.
		/// </summary>
		public const string CurrencyCode = "CURR_CODE";

		/// <summary>
		/// Лицо, от имени которого и за чей счет регистрируется сделка (параметр внебиржевой сделки).
		/// </summary>
		public const string ForAccount = "FOR_ACCOUNT";

		/// <summary>
		/// Все названия инструкций, добавленные в данный момент.
		/// </summary>
		public IEnumerable<string> Names
		{
			get { return Keys; }
		}

		/// <summary>
		/// Получить значение инструкции по имени. Если инструкция не добавлена, то возвращается null.
		/// </summary>
		/// <param name="name">Имя инструкции.</param>
		/// <returns>Значение инструкции.</returns>
		public string GetInstruction(string name)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException("name");

			return this.TryGetValue(name);
		}

		/// <summary>
		/// Получить значение инструкции по имени. Если инструкция не добавлена, то возвращается пустое значение <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">Тип значения.</typeparam>
		/// <param name="name">Имя инструкции.</param>
		/// <returns>Значение инструкции.</returns>
		public T GetInstruction<T>(string name)
		{
			var value = GetInstruction(name);

			if (value == null)
				return default(T);

			if (typeof(T) == typeof(bool))
			{
				return (value == "YES").To<T>();
			}
			else if (typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(decimal))
			{
				return value.To<decimal>().To<T>();
			}
			else if (typeof(T) == typeof(DateTime))
			{
				return value.ToDateTime("yyyyMMdd").To<T>();
			}
			else if (typeof(T) == typeof(TimeSpan))
			{
				return value.ToTimeSpan("HHmmss").To<T>();
			}
			else
				throw new NotSupportedException(LocalizedStrings.Str1844Params.Put(typeof(T)));
		}

		/// <summary>
		/// Установить инструкцию. Если с данным именем ранее уже добавлена инструкция, то применяется новое значение.
		/// </summary>
		/// <param name="name">Название инструкции.</param>
		/// <param name="value">Значение инструкции.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetInstruction(string name, string value)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException("name");

			this[name] = value;
			return this;
		}

		/// <summary>
		/// Установить инструкцию. Если с данным именем ранее уже добавлена инструкция, то применяется новое значение.
		/// </summary>
		/// <typeparam name="T">Тип значения.</typeparam>
		/// <param name="name">Название инструкции.</param>
		/// <param name="value">Значение инструкции.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetInstruction<T>(string name, T value)
		{
			string str;

			if (typeof(T) == typeof(bool))
			{
				str = value.To<bool>() ? "YES" : "NO";
			}
			else if (typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(decimal))
			{
				str = value.ToString();
			}
			else if (typeof(T) == typeof(DateTime))
			{
				str = value.To<DateTime>().ToString("yyyyMMdd");
			}
			else if (typeof(T) == typeof(TimeSpan))
			{
				str = value.To<TimeSpan>().ToString("HHmmss");
			}
			else
				throw new NotSupportedException(LocalizedStrings.Str1844Params.Put(typeof(T)));

			return SetInstruction(name, str);
		}

		/// <summary>
		/// Удалить инструкцию.
		/// </summary>
		/// <param name="name">Название инструкции.</param>
		/// <returns>Транзакция.</returns>
		public Transaction RemoveInstruction(string name)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException("name");

			Remove(name);
			return this;
		}

		/// <summary>
		/// Привести строитель к строковому представлению.
		/// </summary>
		/// <returns>Строковое представление транзакции.</returns>
		public override string ToString()
		{
			var retVal = new StringBuilder();

			foreach (var instruction in this)
				retVal.AppendFormat("{0}={1}; ", instruction.Key, instruction.Value);

			if (retVal.Length > 0)
				retVal.Remove(retVal.Length - 1, 1);

			return retVal.ToString();
		}

		/// <summary>
		/// Привести строитель к строковому представлению для Lua.
		/// </summary>
		/// <returns>Строковое представление транзакции.</returns>
		public string ToLuaString()
		{
			var retVal = new StringBuilder();

			retVal.AppendLine("t = {}");

			foreach (var instruction in this)
				retVal.AppendFormat("t[\"{0}\"] = \"{1}\"{2}", instruction.Key, instruction.Value, Environment.NewLine);

			retVal.AppendLine("return sendTransaction(t)");
			//retVal.AppendLine("res=sendTransaction(t)");
			//retVal.AppendLine("message(res,1)");
			
			return retVal.ToString();
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="TransactionId"/>.
		/// </summary>
		/// <param name="transactionId">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetTransactionId(long transactionId)
		{
			return SetInstruction(TransactionId, transactionId);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="Action"/>.
		/// </summary>
		/// <param name="action">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetAction(string action)
		{
			if (action.IsEmpty())
				throw new ArgumentNullException("action");

			return SetInstruction(Action, action);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="Account"/>.
		/// </summary>
		/// <param name="account">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetAccount(string account)
		{
			if (account.IsEmpty())
				throw new ArgumentNullException("account");

			return SetInstruction(Account, account);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="Side"/>.
		/// </summary>
		/// <param name="side">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetSide(Sides side)
		{
			return SetInstruction(Side, side == Sides.Buy ? "B" : "S");
		}

		///// <summary>
		///// Установить значения для инструкций <see cref="ClassCode"/> и <see cref="SecurityCode"/>.
		///// </summary>
		///// <param name="security">Инструмент.</param>
		///// <returns>Транзакция.</returns>
		//public Transaction SetSecurity(Security security)
		//{
		//	if (security == null)
		//		throw new ArgumentNullException("security");

		//	return 
		//		SetClassCode(security.Class).
		//		SetSecurityCode(security.Code);
		//}

		/// <summary>
		/// Установить значение для инструкции <see cref="ClassCode"/>.
		/// </summary>
		/// <param name="classCode">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetClassCode(string classCode)
		{
			if (classCode.IsEmpty())
				throw new ArgumentNullException("classCode");

			return SetInstruction(ClassCode, classCode);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="BaseContract"/>.
		/// </summary>
		/// <param name="baseContract">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetBaseContract(string baseContract)
		{
			if (baseContract.IsEmpty())
				throw new ArgumentNullException("baseContract");

			return SetInstruction(BaseContract, baseContract);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="SecurityCode"/>.
		/// </summary>
		/// <param name="securityCode">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetSecurityCode(string securityCode)
		{
			if (securityCode.IsEmpty())
				throw new ArgumentNullException("securityCode");

			return SetInstruction(SecurityCode, securityCode);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="ClientCode"/>.
		/// </summary>
		/// <param name="clientCode">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetClientCode(string clientCode)
		{
			if (clientCode.IsEmpty())
				throw new ArgumentNullException("clientCode");

			return SetInstruction(ClientCode, clientCode);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="Type"/>.
		/// </summary>
		/// <param name="type">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetType(OrderTypes type)
		{
			switch (type)
			{
				case OrderTypes.Limit:
					return SetInstruction(Type, "L");
				case OrderTypes.Market:
					return SetInstruction(Type, "M");
				case OrderTypes.Conditional:
					throw new NotSupportedException();
				default:
					throw new ArgumentOutOfRangeException("type", type, LocalizedStrings.Str1845);
			}
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="Volume"/>.
		/// </summary>
		/// <param name="volume">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetVolume(int volume)
		{
			return SetInstruction(Volume, volume);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="VisibleVolume"/>.
		/// </summary>
		/// <param name="volume">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetVisibleVolume(int volume)
		{
			return SetInstruction(VisibleVolume, volume);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="Price"/>.
		/// </summary>
		/// <param name="price">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetPrice(decimal price)
		{
			return SetInstruction(Price, price);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="TimeInForce"/>.
		/// </summary>
		/// <param name="timeInForce">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetTimeInForce(TimeInForce timeInForce)
		{
			string value;

			switch (timeInForce)
			{
				case Messages.TimeInForce.PutInQueue:
					value = "PUT_IN_QUEUE";
					break;
				case Messages.TimeInForce.MatchOrCancel:
					value = "FILL_OR_KILL";
					break;
				case Messages.TimeInForce.CancelBalance:
					value = "KILL_BALANCE";
					break;
				default:
					throw new ArgumentOutOfRangeException("timeInForce", timeInForce, LocalizedStrings.Str1846);
			}

			return SetInstruction(TimeInForce, value);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="StopPrice"/>.
		/// </summary>
		/// <param name="price">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetStopPrice(decimal price)
		{
			return SetInstruction(StopPrice, price);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="ExpiryDate"/>.
		/// </summary>
		/// <param name="time">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetExpiryDate(DateTimeOffset time)
		{
			if (time == DateTimeOffset.MaxValue)
				return SetInstruction(ExpiryDate, "GTC");
			else if (time.Date == DateTimeOffset.Now.Date)
				return SetInstruction(ExpiryDate, "TODAY");
			else
				return SetInstruction(ExpiryDate, time.ToLocalTime(TimeHelper.Moscow));
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="StopLimitPrice"/>.
		/// </summary>
		/// <param name="price">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetStopLimitPrice(decimal price)
		{
			return SetInstruction(StopLimitPrice, price);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="MarketTakeProfit"/>.
		/// </summary>
		/// <param name="isMarketTakeProfit">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetMarketTakeProfit(bool isMarketTakeProfit)
		{
			return SetInstruction(MarketTakeProfit, isMarketTakeProfit);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="MarketStopLimit"/>.
		/// </summary>
		/// <param name="isMarketStopLimit">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetMarketStopLimit(bool isMarketStopLimit)
		{
			return SetInstruction(MarketStopLimit, isMarketStopLimit);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="Comment"/>.
		/// </summary>
		/// <param name="comment">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetComment(string comment)
		{
			if (comment.IsEmpty())
				throw new ArgumentNullException("comment");

			return SetInstruction(Comment, comment);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="OffsetValue"/> и <see cref="OffsetUnit"/>.
		/// </summary>
		/// <param name="offset">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetOffset(Unit offset)
		{
			if (offset == null)
				throw new ArgumentNullException("offset");

			return	SetInstruction(OffsetValue, offset.Value).
					SetInstruction(OffsetUnit, offset.Type == UnitTypes.Percent ? "PERCENTS" : "PRICE_UNITS");
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="SpreadValue"/> и <see cref="SpreadUnit"/>.
		/// </summary>
		/// <param name="spread">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetSpread(Unit spread)
		{
			if (spread == null)
				throw new ArgumentNullException("spread");

			return	SetInstruction(SpreadValue, spread.Value).
					SetInstruction(SpreadUnit, spread.Type == UnitTypes.Percent ? "PERCENTS" : "PRICE_UNITS");
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="ConditionOrderId"/>.
		/// </summary>
		/// <param name="conditionOrderId">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetConditionOrderId(long conditionOrderId)
		{
			return SetInstruction(ConditionOrderId, conditionOrderId);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="ConditionOrderUseMatchedBalance"/>.
		/// </summary>
		/// <param name="conditionOrderUseMatchedBalance">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetConditionOrderUseMatchedBalance(bool conditionOrderUseMatchedBalance)
		{
			return SetInstruction(ConditionOrderUseMatchedBalance, conditionOrderUseMatchedBalance);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="ConditionOrderPartiallyMatched"/>.
		/// </summary>
		/// <param name="conditionOrderPartiallyMatched">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetConditionOrderPartiallyMatched(bool conditionOrderPartiallyMatched)
		{
			return SetInstruction(ConditionOrderPartiallyMatched, conditionOrderPartiallyMatched);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="IsActiveInTime"/>.
		/// </summary>
		/// <param name="isActiveInTime">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetIsActiveInTime(bool isActiveInTime)
		{
			return SetInstruction(IsActiveInTime, isActiveInTime);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="ActiveFrom"/>.
		/// </summary>
		/// <param name="time">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetActiveFrom(DateTime time)
		{
			return SetInstruction(ActiveFrom, time);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="ActiveTo"/>.
		/// </summary>
		/// <param name="time">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetActiveTo(DateTime time)
		{
			return SetInstruction(ActiveTo, time);
		}

		/// <summary>
		/// Установить значения для инструкций <see cref="OtherSecurityClassCode"/> и <see cref="OtherSecurityCode"/>.
		/// </summary>
		/// <param name="secCode">Код инструмента.</param>
		/// <param name="secClass">Класс инструмента</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetOtherSecurity(string secCode, string secClass)
		{
			if (secCode == null)
				throw new ArgumentNullException("secCode");

			if (secClass == null)
				throw new ArgumentNullException("secClass");

			return
				SetInstruction(OtherSecurityClassCode, secClass).
				SetInstruction(OtherSecurityCode, secCode);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="LinkedOrderPrice"/>.
		/// </summary>
		/// <param name="linkedOrderPrice">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetLinkedOrderPrice(decimal linkedOrderPrice)
		{
			return SetInstruction(LinkedOrderPrice, linkedOrderPrice);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="LinkedOrderCancel"/>.
		/// </summary>
		/// <param name="cancel">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetLinkedOrderCancel(bool cancel)
		{
			return SetInstruction(LinkedOrderCancel, cancel);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="StopOrderKind"/>.
		/// </summary>
		/// <param name="stopOrderKind">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetStopOrderKind(string stopOrderKind)
		{
			if (stopOrderKind.IsEmpty())
				throw new ArgumentNullException("stopOrderKind");

			return SetInstruction(StopOrderKind, stopOrderKind);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="StopPriceCondition"/>.
		/// </summary>
		/// <param name="stopPriceCondition">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetStopPriceCondition(string stopPriceCondition)
		{
			if (stopPriceCondition.IsEmpty())
				throw new ArgumentNullException("stopPriceCondition");

			return SetInstruction(StopPriceCondition, stopPriceCondition);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="StopOrderId"/>.
		/// </summary>
		/// <param name="orderId">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetStopOrderId(long orderId)
		{
			return SetInstruction(StopOrderId, orderId);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="OrderId"/>.
		/// </summary>
		/// <param name="orderId">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetOrderId(long orderId)
		{
			return SetInstruction(OrderId, orderId);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="FortsMode"/>.
		/// </summary>
		/// <param name="modeId">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetFortsMode(int modeId)
		{
			return SetInstruction(FortsMode, modeId);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="FirstOrderId"/>.
		/// </summary>
		/// <param name="orderId">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetFirstOrderId(long orderId)
		{
			return SetInstruction(FirstOrderId, orderId);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="FirstOrderNewPrice"/>.
		/// </summary>
		/// <param name="price">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetFirstOrderPrice(decimal price)
		{
			return SetInstruction(FirstOrderNewPrice, price);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="SetFirstVolume"/>.
		/// </summary>
		/// <param name="volume">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetFirstVolume(int volume)
		{
			return SetInstruction(FirstOrderNewVolume, volume);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="Partner"/>.
		/// </summary>
		/// <param name="partner">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetPartner(string partner)
		{
			return SetInstruction(Partner, partner);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="RepoTerm"/>.
		/// </summary>
		/// <param name="days">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetRepoTerm(int days)
		{
			return SetInstruction(RepoTerm, days);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="RepoRate"/>.
		/// </summary>
		/// <param name="percents">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetRepoRate(int percents)
		{
			return SetInstruction(RepoRate, percents);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="BlockSecurities"/>.
		/// </summary>
		/// <param name="block">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetBlockSecurities(bool block)
		{
			return SetInstruction(BlockSecurities, block);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="RefundRate"/>.
		/// </summary>
		/// <param name="percents">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetRefundRate(int percents)
		{
			return SetInstruction(RefundRate, percents);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="MatchRef"/>.
		/// </summary>
		/// <param name="refference">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetMatchRef(string refference)
		{
			return SetInstruction(MatchRef, refference);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="SettleCode"/>.
		/// </summary>
		/// <param name="code">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetSettleCode(string code)
		{
			return SetInstruction(SettleCode, code);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="SecondPrice"/>.
		/// </summary>
		/// <param name="price">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetSecondPrice(decimal price)
		{
			return SetInstruction(SecondPrice, price);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="SettleDate"/>.
		/// </summary>
		/// <param name="date">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetSettleDate(DateTime date)
		{
			return SetInstruction(SettleDate, date);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="StartDiscount"/>.
		/// </summary>
		/// <param name="percents">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetStartDiscount(int percents)
		{
			return SetInstruction(StartDiscount, percents);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="LowerDiscount"/>.
		/// </summary>
		/// <param name="percents">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetLowerDiscount(int percents)
		{
			return SetInstruction(LowerDiscount, percents);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="UpperDiscount"/>.
		/// </summary>
		/// <param name="percents">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetUpperDiscount(int percents)
		{
			return SetInstruction(UpperDiscount, percents);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="CurrencyCode"/>.
		/// </summary>
		/// <param name="code">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetCurrencyCode(string code)
		{
			return SetInstruction(CurrencyCode, code);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="RepoValue"/>.
		/// </summary>
		/// <param name="value">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetRepoValue(decimal value)
		{
			return SetInstruction(RepoValue, value);
		}

		/// <summary>
		/// Установить значение для инструкции <see cref="ForAccount"/>.
		/// </summary>
		/// <param name="value">Значение.</param>
		/// <returns>Транзакция.</returns>
		public Transaction SetForAccount(string value)
		{
			return SetInstruction(ForAccount, value);
		}
	}
}