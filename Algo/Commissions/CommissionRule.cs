namespace StockSharp.Algo.Commissions
{
	using System;
	using System.ComponentModel;
	using System.Runtime.Serialization;
	using DataContract = System.Runtime.Serialization.DataContractAttribute;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Правило вычисления комиссии.
	/// </summary>
	[DataContract]
	public abstract class CommissionRule : NotifiableObject, ICommissionRule
	{
		/// <summary>
		/// Инициализировать <see cref="CommissionRule"/>.
		/// </summary>
		protected CommissionRule()
		{
		}

		private Unit _value = new Unit();

		/// <summary>
		/// Значение комиссии.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str159Key)]
		[DescriptionLoc(LocalizedStrings.CommissionValueKey)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public Unit Value
		{
			get { return _value; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_value = value;
				NotifyChanged("Value");
			}
		}

		/// <summary>
		/// Суммарное значение комиссии.
		/// </summary>
		[Browsable(false)]
		public decimal Commission { get; private set; }

		private string _title;

		/// <summary>
		/// Заголовок.
		/// </summary>
		[Browsable(false)]
		public string Title
		{
			get { return _title; }
			protected set
			{
				_title = value;
				NotifyChanged("Title");
			}
		}

		/// <summary>
		/// Сбросить состояние.
		/// </summary>
		public virtual void Reset()
		{
			Commission = 0;
		}

		/// <summary>
		/// Рассчитать комиссию.
		/// </summary>
		/// <param name="message">Сообщение, содержащее информацию по заявке или собственной сделке.</param>
		/// <returns>Комиссия. Если комиссию рассчитать невозможно, то будет возвращено <see langword="null"/>.</returns>
		public decimal? ProcessExecution(ExecutionMessage message)
		{
			var commission = OnProcessExecution(message);

			if (commission != null)
				Commission += commission.Value;

			return commission;
		}

		/// <summary>
		/// Рассчитать комиссию.
		/// </summary>
		/// <param name="message">Сообщение, содержащее информацию по заявке или собственной сделке.</param>
		/// <returns>Комиссия. Если комиссию рассчитать невозможно, то будет возвращено <see langword="null"/>.</returns>
		protected abstract decimal? OnProcessExecution(ExecutionMessage message);

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public virtual void Load(SettingsStorage storage)
		{
			Value = storage.GetValue<Unit>("Value");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public virtual void Save(SettingsStorage storage)
		{
			storage.SetValue("Value", Value);
		}
	}

	/// <summary>
	/// Комиссия за заявку.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str504Key)]
	[DescriptionLoc(LocalizedStrings.Str660Key)]
	public class CommissionPerOrderRule : CommissionRule
	{
		/// <summary>
		/// Рассчитать комиссию.
		/// </summary>
		/// <param name="message">Сообщение, содержащее информацию по заявке или собственной сделке.</param>
		/// <returns>Комиссия. Если комиссию рассчитать невозможно, то будет возвращено <see langword="null"/>.</returns>
		protected override decimal? OnProcessExecution(ExecutionMessage message)
		{
			if (message.ExecutionType == ExecutionTypes.Order)
				return (decimal)Value;
			
			return null;
		}
	}

	/// <summary>
	/// Комиссия за сделку.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str506Key)]
	[DescriptionLoc(LocalizedStrings.Str661Key)]
	public class CommissionPerTradeRule : CommissionRule
	{
		/// <summary>
		/// Рассчитать комиссию.
		/// </summary>
		/// <param name="message">Сообщение, содержащее информацию по заявке или собственной сделке.</param>
		/// <returns>Комиссия. Если комиссию рассчитать невозможно, то будет возвращено <see langword="null"/>.</returns>
		protected override decimal? OnProcessExecution(ExecutionMessage message)
		{
			if (message.ExecutionType == ExecutionTypes.Trade)
				return (decimal)Value;
			
			return null;
		}
	}

	/// <summary>
	/// Комиссия за объем в заявке.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str662Key)]
	[DescriptionLoc(LocalizedStrings.Str663Key)]
	public class CommissionPerOrderVolumeRule : CommissionRule
	{
		/// <summary>
		/// Рассчитать комиссию.
		/// </summary>
		/// <param name="message">Сообщение, содержащее информацию по заявке или собственной сделке.</param>
		/// <returns>Комиссия. Если комиссию рассчитать невозможно, то будет возвращено <see langword="null"/>.</returns>
		protected override decimal? OnProcessExecution(ExecutionMessage message)
		{
			if (message.ExecutionType == ExecutionTypes.Order)
				return (decimal)(message.Volume * Value);
			
			return null;
		}
	}

	/// <summary>
	/// Комиссия за объем в сделке.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str664Key)]
	[DescriptionLoc(LocalizedStrings.Str665Key)]
	public class CommissionPerTradeVolumeRule : CommissionRule
	{
		/// <summary>
		/// Рассчитать комиссию.
		/// </summary>
		/// <param name="message">Сообщение, содержащее информацию по заявке или собственной сделке.</param>
		/// <returns>Комиссия. Если комиссию рассчитать невозможно, то будет возвращено <see langword="null"/>.</returns>
		protected override decimal? OnProcessExecution(ExecutionMessage message)
		{
			if (message.ExecutionType == ExecutionTypes.Trade)
				return (decimal)(message.Volume * Value);
			
			return null;
		}
	}

	/// <summary>
	/// Комиссия за количество заявок.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str666Key)]
	[DescriptionLoc(LocalizedStrings.Str667Key)]
	public class CommissionPerOrderCountRule : CommissionRule
	{
		private int _currentCount;
		private int _count;

		/// <summary>
		/// Количество заявок.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str668Key)]
		[DescriptionLoc(LocalizedStrings.Str669Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public int Count
		{
			get { return _count; }
			set
			{
				_count = value;
				Title = value.To<string>();
			}
		}

		/// <summary>
		/// Сбросить состояние.
		/// </summary>
		public override void Reset()
		{
			_currentCount = 0;
			base.Reset();
		}

		/// <summary>
		/// Рассчитать комиссию.
		/// </summary>
		/// <param name="message">Сообщение, содержащее информацию по заявке или собственной сделке.</param>
		/// <returns>Комиссия. Если комиссию рассчитать невозможно, то будет возвращено <see langword="null"/>.</returns>
		protected override decimal? OnProcessExecution(ExecutionMessage message)
		{
			if (message.ExecutionType != ExecutionTypes.Order)
				return null;

			if (++_currentCount < Count)
				return null;

			_currentCount = 0;
			return (decimal)Value;
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("Count", Count);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Count = storage.GetValue<int>("Count");
		}
	}

	/// <summary>
	/// Комиссия за количество сделок.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str670Key)]
	[DescriptionLoc(LocalizedStrings.Str671Key)]
	public class CommissionPerTradeCountRule : CommissionRule
	{
		private int _currentCount;
		private int _count;

		/// <summary>
		/// Количество сделок.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.TradesOfKey)]
		[DescriptionLoc(LocalizedStrings.Str232Key, true)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public int Count
		{
			get { return _count; }
			set
			{
				_count = value;
				Title = value.To<string>();
			}
		}

		/// <summary>
		/// Сбросить состояние.
		/// </summary>
		public override void Reset()
		{
			_currentCount = 0;
			base.Reset();
		}

		/// <summary>
		/// Рассчитать комиссию.
		/// </summary>
		/// <param name="message">Сообщение, содержащее информацию по заявке или собственной сделке.</param>
		/// <returns>Комиссия. Если комиссию рассчитать невозможно, то будет возвращено <see langword="null"/>.</returns>
		protected override decimal? OnProcessExecution(ExecutionMessage message)
		{
			if (message.ExecutionType != ExecutionTypes.Trade)
				return null;

			if (++_currentCount < Count)
				return null;

			_currentCount = 0;
			return (decimal)Value;
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("Count", Count);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Count = storage.GetValue<int>("Count");
		}
	}

	/// <summary>
	/// Комиссия за цену в сделке.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str672Key)]
	[DescriptionLoc(LocalizedStrings.Str673Key)]
	public class CommissionPerTradePriceRule : CommissionRule
	{
		/// <summary>
		/// Рассчитать комиссию.
		/// </summary>
		/// <param name="message">Сообщение, содержащее информацию по заявке или собственной сделке.</param>
		/// <returns>Комиссия. Если комиссию рассчитать невозможно, то будет возвращено <see langword="null"/>.</returns>
		protected override decimal? OnProcessExecution(ExecutionMessage message)
		{
			if (message.ExecutionType == ExecutionTypes.Trade)
				return (decimal)(message.TradePrice * message.Volume * Value);
			
			return null;
		}
	}

	/// <summary>
	/// Комиссия инструмента.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.SecurityKey)]
	[DescriptionLoc(LocalizedStrings.Str674Key)]
	public class CommissionSecurityIdRule : CommissionRule
	{
		private SecurityId _securityId;

		/// <summary>
		/// Идентификатор инструмента.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.SecurityIdKey)]
		[DescriptionLoc(LocalizedStrings.SecurityIdKey, true)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public SecurityId SecurityId
		{
			get { return _securityId; }
			set
			{
				_securityId = value;
				Title = value.ToString();
			}
		}

		/// <summary>
		/// Рассчитать комиссию.
		/// </summary>
		/// <param name="message">Сообщение, содержащее информацию по заявке или собственной сделке.</param>
		/// <returns>Комиссия. Если комиссию рассчитать невозможно, то будет возвращено <see langword="null"/>.</returns>
		protected override decimal? OnProcessExecution(ExecutionMessage message)
		{
			if (message.ExecutionType == ExecutionTypes.Trade && message.SecurityId == SecurityId)
				return (decimal)Value;
			
			return null;
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("SecurityId", SecurityId);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			SecurityId = storage.GetValue<SecurityId>("SecurityId");
		}
	}

	/// <summary>
	/// Комиссия типа инструмента.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str675Key)]
	[DescriptionLoc(LocalizedStrings.Str676Key)]
	public class CommissionSecurityTypeRule : CommissionRule
	{
		/// <summary>
		/// Создать <see cref="CommissionSecurityTypeRule"/>.
		/// </summary>
		public CommissionSecurityTypeRule()
		{
			SecurityType = SecurityTypes.Stock;
		}

		private SecurityTypes _securityType;

		/// <summary>
		/// Тип инструмента.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.TypeKey)]
		[DescriptionLoc(LocalizedStrings.Str360Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public SecurityTypes SecurityType
		{
			get { return _securityType; }
			set
			{
				_securityType = value;
				Title = value.ToString();
			}
		}

		/// <summary>
		/// Рассчитать комиссию.
		/// </summary>
		/// <param name="message">Сообщение, содержащее информацию по заявке или собственной сделке.</param>
		/// <returns>Комиссия. Если комиссию рассчитать невозможно, то будет возвращено <see langword="null"/>.</returns>
		protected override decimal? OnProcessExecution(ExecutionMessage message)
		{
			if (message.ExecutionType == ExecutionTypes.Trade && message.SecurityId.SecurityType == SecurityType)
				return (decimal)Value;
			
			return null;
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("SecurityType", SecurityType);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			SecurityType = storage.GetValue<SecurityTypes>("SecurityType");
		}
	}

	/// <summary>
	/// Комиссия площадки.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.BoardKey)]
	[DescriptionLoc(LocalizedStrings.BoardCommissionKey)]
	public class CommissionBoardCodeRule : CommissionRule
	{
		private string _boardCode;

		/// <summary>
		/// Код площадки.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.BoardKey)]
		[DescriptionLoc(LocalizedStrings.BoardCodeKey)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public string BoardCode
		{
			get { return _boardCode; }
			set
			{
				_boardCode = value;
				Title = value;
			}
		}

		/// <summary>
		/// Рассчитать комиссию.
		/// </summary>
		/// <param name="message">Сообщение, содержащее информацию по заявке или собственной сделке.</param>
		/// <returns>Комиссия. Если комиссию рассчитать невозможно, то будет возвращено <see langword="null"/>.</returns>
		protected override decimal? OnProcessExecution(ExecutionMessage message)
		{
			if (message.ExecutionType == ExecutionTypes.Trade && message.SecurityId.BoardCode.CompareIgnoreCase(BoardCode))
				return (decimal)Value;
			
			return null;
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("BoardCode", BoardCode);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			BoardCode = storage.GetValue<string>("BoardCode");
		}
	}

	/// <summary>
	/// Комиссия за оборот.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.TurnoverKey)]
	[DescriptionLoc(LocalizedStrings.TurnoverCommissionKey)]
	public class CommissionTurnOverRule : CommissionRule
	{
		private decimal _currentTurnOver;
		private decimal _turnOver;

		/// <summary>
		/// Оборот.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.TurnoverKey)]
		[DescriptionLoc(LocalizedStrings.TurnoverKey, true)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public decimal TurnOver
		{
			get { return _turnOver; }
			set
			{
				_turnOver = value;
				Title = value.To<string>();
			}
		}

		/// <summary>
		/// Сбросить состояние.
		/// </summary>
		public override void Reset()
		{
			_turnOver = 0;
			base.Reset();
		}

		/// <summary>
		/// Рассчитать комиссию.
		/// </summary>
		/// <param name="message">Сообщение, содержащее информацию по заявке или собственной сделке.</param>
		/// <returns>Комиссия. Если комиссию рассчитать невозможно, то будет возвращено <see langword="null"/>.</returns>
		protected override decimal? OnProcessExecution(ExecutionMessage message)
		{
			if (message.ExecutionType != ExecutionTypes.Trade)
				return null;

			_currentTurnOver += message.TradePrice * message.Volume;

			if (_currentTurnOver < TurnOver)
				return null;

			return (decimal)Value;
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("TurnOver", TurnOver);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			TurnOver = storage.GetValue<decimal>("TurnOver");
		}
	}
}